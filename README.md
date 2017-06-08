# RelaSharp

## Overview

RelaSharp is a tool I developed for verifying lock and wait-free algorithms in C#. It is heavily inspired by Relacy (hence the name RelaSharp) and CHESS. I mostly wrote it in order to get a better feel for memory models in general, but it is also genuinely useful for vastly increasing
confidence in lock-free code, or even exhaustively verifying that the code is correct.

RelaSharp is aimed at intricate tests of small lock-free algorithms, it's not aimed at large applications (and currently requires some source level instrumentation, anyway).

## Example

Perhaps the simplest example of the type of checking RelaSharp performs is demonstrated by two threads:

```csharp
volatile int x0 = 0, y0 = 0, x1 = 0, y1 = 0;

void Thread1()
{
    x0 = 1;
    y0 = x1;
}

void Thread2()
{
    x1 = 1;
    y1 = x0;
}
```

At termination, according to the C# memory model, it's possible to observe both y0 and y1 zero. In fact, I have a separate repo demonstrating just [this](https://github.com/nicknash/StoreLoad).
If the above two threads are run in a suitable RelaSharp test (such as [this](Examples/StoreLoad.cs)), it will produce the following output:

```
Test failed with reason: 'Both of y0 and y1 are zero! (store load reordering!)'

Code executed in directories: /Users/nick/Core/Current/csharp/RelaSharp/Examples

Interleaved execution log
*************************
[0] 0@1 in StoreLoad.cs:Thread1:36 | Store (AcquireRelease) --> 1
[1] 0@2 in StoreLoad.cs:Thread1:37 | Load (AcquireRelease) <-- 0
[2] 1@1 in StoreLoad.cs:Thread2:42 | Store (AcquireRelease) --> 1
[3] 1@2 in StoreLoad.cs:Thread2:43 | Load (AcquireRelease) <-- 0
[4] 1@2 in StoreLoad.cs:OnFinished:50 | Assert (failed): Both of y0 and y1 are zero! (store load reordering!)

Individual thread logs
**********************
Thread 0
--------
[0] 0@1 in StoreLoad.cs:Thread1:36 | Store (AcquireRelease) --> 1
[1] 0@2 in StoreLoad.cs:Thread1:37 | Load (AcquireRelease) <-- 0
Thread 1
--------
[2] 1@1 in StoreLoad.cs:Thread2:42 | Store (AcquireRelease) --> 1
[3] 1@2 in StoreLoad.cs:Thread2:43 | Load (AcquireRelease) <-- 0
[4] 1@2 in StoreLoad.cs:OnFinished:50 | Assert (failed): Both of y0 and y1 are zero! (store load reordering!)
```

The output shows the loads and stores of the threads, with their line numbers and the values they stored or loaded.

This repo contains quite a few more examples, some illustrative ones are: 

* A [Petersen lock](Examples/Petersen.cs) 
* [Treiber's stack](Examples/TreiberStack.cs)
* A [Michael-Scott queue](Examples/MichaelScottQueue.cs) 
* A bounded wait-free [SPSC queue](Examples/BoundedSPSCQueue.cs). 


## Features

Roughly the things that RelaSharp does is:

* Generate executions consistent with the C++11 memory model except consume semantics, or the C# memory model.
* Race detection.
* Dead-lock detection (including lost thread wake-ups), when exclusive locks are in use.
* Live-lock detection: When a very long execution is generated, it is heuristically classified as a live-lock.
* Random thread scheduling: At every instrumented instruction, possibly generate a pre-emption.
* Exhaustive "fair demonic" scheduling: Depth-first search of all thread interleavings, avoiding false divergence into live-lock.
* Execution tracing: Detailed output showing file/line number and the results of every volatile load/store, interlocked instruction, etc.

## Basic Algorithm

RelaSharp works by wrapping all memory model related instructions in its own types, for example, RInterlocked.CompareExchange 
wraps Interlocked.CompareExchange. This allows it to do three main things:

* Generate pre-emption points for the scheduler
* Generate results for the instruction (store, load, CAS, etc) that are consistent with the execution so far, but more likely to include
  a memory re-ordering than running the code natively would allow. 
* Record file/line numbers and results of the execution for review if the test fails.

## Memory Model Simulation via Vector Clocks

This section gives a very rough description of how RelaSharp simulates the C++11 memory model (which the C# memory model can be defined as a special case of). The description below is quite imprecise, and really gives a rough impression of how things work. The entirety of the description below really just corresponds to some very simple code in a single file: [AccessHistory.cs](MemoryModel/AccessHistory.cs) 

To simulate the memory-model, RelaSharp assigns threads incrementing time-stamps at pre-emption points. At each store, the time-stamp of the storing thread is recorded. An ordered, bounded history of all stores to each instrumented variable is also maintained. When a thread performs a load of an instrumented variable V, it considers the stores in this history. Let's call this history H(V). For a load of V, each element H(V) is referred to as a  _candidate load result_ for V, and we say that it was created by a _candidate store_. For a given load of V by some thread, some elements of H(V) will be inconsistent with the memory model semantics, and cannot be returned. Thus we need an algorithm that given a load of V returns an element of H(V) that is consistent with the memory model semantics.

In selecting a candidate load result, candidate stores are considered in the reverse order that they were performed. In C++11 parlance, this is possible because there is a single total "modification order" associated with any atomic variable. With this newer-to-older ordering of stores to V in mind, there are four main cases to consider in order to select a candidate load result to answer a load by a given thread. These can be very roughly stated as follows (R refers to the current candidate load result for V under consideration):

1. The load has sequentially consistent semantics, and the candidate store that created R was sequentially consistent. In this case, the load can only be answered with R, as any other result would violate sequential consistency.
2. Otherwise, if the loading thread has seen any other store by the storing thread that created R that is as-new or newer-than the store (by that thread) that created R, then the load must be answered with R.
3. Otherwise, if the loading thread has seen any other store by any thread that is as new or newer-than than that thread's time-stamp the last time it loaded R then the load must be answered with R. This is a bit of a mouthful. What this condition says is that, if the loading thread has seen a store, S, of another thread that is later than the last load of R by that other thread, then returning an older candidate load result than R would be
incorrect because it would be as though the loading thread had not seen S yet after all. 
4. If none of the above hold, an older load from the history can be returned.

These four cases leave the meaning of "seeing a store" undefined. A more precise way of saying thread A has "seen a store" by thread B, is 
to say that thread A has performed a load with acquire semantics of some atomic variable that B performed a store with release semantics to. Abusing terminology a little, the load-acquire is said to "synchronize-with" the store-release. The point of a synchronize-with relationship is that everything that has happened before the store-release by B, is guaranteed to have happened before everything after the load-acquire by A.

RelaSharp uses a simple vector clock algorithm to track these synchronized-with relationships. For a test involving N threads, it creates a vector clock, N entry vector clock for each thread. Each thread also has an internal clock incremented at each atomic operation. For a given thread T1, VC[T2] is the latest value of thread T2's clock that T1 has synchronized with. 

The main way that the memory-model semantics are respected is then by appropriately "joining" vector clocks. To join two vector clocks VC1 and VC2 just means to create a new vector clock that is the pair-wise max of the elements. So for example, at a load-acquire, a thread's vector clock is joined with the vector clock of the storing thread at the time it performed the store-release. This joining implies that transitive relationships of happens-before / synchronized-with are tracked correctly.

There is a little more to getting the details of the above right, but hopefully the sketch above gives a reasonable idea to anyone who wants to take a look at the code.

## Generic Interface

RelaSharp provides a C++11-like atomic<> interface. For C# programmers, this is perhaps a lower-level interface than they want. As a result the
atomic<> like interface is wrapped in a C#-like interface (of Interlocked, Volatile, etc) described in the next section. This interface to RelaSharp is a little strange in that it allows simulating more relaxed memory behaviours than C# actually allows, e.g., in C# all stores have release semantics, this atomic<> interface allows simulating what it would be like if that didn't hold. Really the reason this low-level interface exists is that it is a natural, well-documented (thanks to C++11) set of operations to define the C# memory model in terms of.

A representative subset of it looks something like this:

```csharp
var myAtomic = new Atomic<int>();
myAtomic.Store(123, MemoryOrder.Relaxed);
myAtomic.Load(MemoryOrder.SequentiallyConsistent);
bool success = myAtomic.CompareExchange(456, 134, MemoryOrder.AcquireRelease);
```

The last argument to these atomic<> functions is the memory order, the supported memory orders, found in [MemoryOrder.cs](MemoryOrder.cs):
```csharp
enum MemoryOrder
{
    Relaxed,
    Acquire,
    Release,
    AcquireRelease,
    SequentiallyConsistent
}
```



## C# Specific Interface

## Scheduling

## Limitations

## Possible Enhancements

## Related Tools

## Command Line Examples

This repo contains a simple [command line utility](EntryPoint/RunExamples.cs) for exploring some sample RelaSharp tests.
It's hopefully pretty self explanatory and just allows tweaking some knobs of RelaSharp and running it against some built-in examples.

For example, running it without command-line arguments:

```
➜  RelaSharp git:(master) ✗ dotnet run -- 
Project RelaSharp (.NETCoreApp,Version=v1.1) was previously compiled. Skipping compilation.
Usage:
--quiet             Suppress output of execution logs (defaults to false)
--iterations=X      For random scheduler only: Run for X iterations (defaults to 10000)
--self-test         Run self test mode (suppress all output and only report results that differ from expected results)
--tag=X             Run examples whose name contain the tag (case insensitive, run all examples if unspecified)
--scheduling=X      Use the specified scheduling algorithm, available options are 'random' and 'exhaustive' (defaults to Random)
--live-lock=X       Report executions longer than X as live locks (defaults to 5000)
--list-examples     List the tags of the available examples with their full names (and then exit).
--yield-penalty=X   Exhaustive scheduler: Control the chance of a false-divergent execution, larger implies closer to sequential consistency close to scheduler yields (defaults to 4)
--help              Print this message and exit
```

Selecting an example and seeing results can be done as follows:

```
➜  RelaSharp git:(master) ✗ dotnet run -- --tag=petersen
Project RelaSharp (.NETCoreApp,Version=v1.1) was previously compiled. Skipping compilation.
Running example [4/11]: Petersen: Petersen Mutex
***** Current configuration for 'Petersen Mutex' is 'All operations acquire-release', this is expected to fail
Example failed on iteration number: 21
Not to worry, this failure was expected
----- Begin Test Execution Log ----

Test failed with reason: 'Mutual exclusion not achieved, 2 threads currently in critical section!'

Code executed in directories: /Users/nick/Core/Current/csharp/RelaSharp/Examples

Interleaved execution log
*************************
[0] 0@1 in Petersen.cs:Thread0:47 | Store (Relaxed) --> 1
[1] 1@1 in Petersen.cs:Thread1:65 | Store (Relaxed) --> 1
[2] 1@2 in Petersen.cs:Thread1:72 | Store (Relaxed) --> 1
[3] 0@2 in Petersen.cs:Thread0:54 | Store (Relaxed) --> 0
[4] 1@3 in Petersen.cs:Thread1:74 | Load (Relaxed) <-- 1
[5] 0@3 in Petersen.cs:Thread0:56 | Load (Relaxed) <-- 0
[6] 0@4 in Petersen.cs:Thread0:56 | Load (Relaxed) <-- 0
[7] 0@4 in Petersen.cs:Thread0:58 | Assert (passed): Mutual exclusion not achieved, 1 threads currently in critical section!
[8] 1@4 in Petersen.cs:Thread1:74 | Load (Relaxed) <-- 0
[9] 1@4 in Petersen.cs:Thread1:76 | Assert (failed): Mutual exclusion not achieved, 2 threads currently in critical section!
[10] 0@5 in Petersen.cs:Thread0:59 | Store (Relaxed) --> 0

Individual thread logs
**********************
Thread 0
--------
[0] 0@1 in Petersen.cs:Thread0:47 | Store (Relaxed) --> 1
[3] 0@2 in Petersen.cs:Thread0:54 | Store (Relaxed) --> 0
[5] 0@3 in Petersen.cs:Thread0:56 | Load (Relaxed) <-- 0
[6] 0@4 in Petersen.cs:Thread0:56 | Load (Relaxed) <-- 0
[7] 0@4 in Petersen.cs:Thread0:58 | Assert (passed): Mutual exclusion not achieved, 1 threads currently in critical section!
[10] 0@5 in Petersen.cs:Thread0:59 | Store (Relaxed) --> 0
Thread 1
--------
[1] 1@1 in Petersen.cs:Thread1:65 | Store (Relaxed) --> 1
[2] 1@2 in Petersen.cs:Thread1:72 | Store (Relaxed) --> 1
[4] 1@3 in Petersen.cs:Thread1:74 | Load (Relaxed) <-- 1
[8] 1@4 in Petersen.cs:Thread1:74 | Load (Relaxed) <-- 0
[9] 1@4 in Petersen.cs:Thread1:76 | Assert (failed): Mutual exclusion not achieved, 2 threads currently in critical section!
----- End Test Execution Log ----
Tested 9832.70879300965 operations per second (21 iterations at 709.576923206882 iterations per second) for 0.0295951 seconds.
..........................
***** Current configuration for 'Petersen Mutex' is 'All operations sequentially consistent', this is expected to pass
No failures after 10000 iterations
That's good, this example was expected to pass.
Tested 60474.040480368 operations per second (10000 iterations at 4602.53137384549 iterations per second) for 2.1727174 seconds.
..........................
***** Current configuration for 'Petersen Mutex' is 'Relaxed flag entry, Release flag exit, Acquire flag spin, acquire-release exchange on victim.', this is expected to pass
No failures after 10000 iterations
That's good, this example was expected to pass.
Tested 78407.8670901449 operations per second (10000 iterations at 4596.89547744552 iterations per second) for 2.1753812 seconds.
..........................
```

If you want to play around, the list of examples I've implemented can be listed:

```
➜  RelaSharp git:(master) ✗ dotnet run -- --list-examples
Project RelaSharp (.NETCoreApp,Version=v1.1) was previously compiled. Skipping compilation.
Available examples:
-------------------
SimpleAcquireRelease    Simple demonstration of acquire and release semantics.
StoreLoad       	Store Load Re-ordering example
SPSC    		SPSC queue tests
Petersen		Petersen Mutex
TotalOrder      	Total order test (multiple-copy atomicity / write synchronization test)
LiveLock		Livelock example (fragment of Petersen lock)
Treiber 		Treiber Stack
MichaelScott    	Michael-Scott Queue
Deadlock		Deadlock detection example
LostWakeUp      	Lost Wake Up example
LeftRight       	Left-Right Synchronization Primitive Example
```


## References


