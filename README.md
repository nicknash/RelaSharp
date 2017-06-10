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
These have the usual meanings as in the C++11 memory model. Notably, consume semantics are absent. These would be a big challenge to implement, as RelaSharp has no awareness of data / control dependencies. Luckily, no such concept exists in C#, and so its memory model can still easily be simulated.

## C# Specific Interface

The C#-like interface to RelaSharp should have a familiar flavour to C# programmers, here is a snippet:

```csharp
RInterlocked.Exchange(ref _readIndex, nextReadIndex);
RVolatile.Write(ref _readIndex, nextReadIndex);
RUnordered.Write(ref _readIndex, nextReadIndex);
```
The functions in RInterlocked and RVolatile serve as drop-in replacements for Interlocked and Volatile. The only unusual looking function here is 
RUnordered.Write. This is specifying that no ordering above the default C# memory model is being placed on the write. This required so that RelaSharp can track accesses to _readIndex. Arguably, the use of RUnordered makes the code a little more explicit anyway, as it is clear that a memory ordering was not forgotten, but was explicitly annotated as not required.

The usual locking / condition variable behaviour via C#'s Monitor is also available in RelaSharp, here is an excerpt from a [deadlock detection example](Examples/Deadlock.cs):

```csharp
RMonitor.Enter(myLock);
RMonitor.Enter(nextLock);
RMonitor.Exit(nextLock);
RMonitor.Exit(myLock);
```

Translation from C# to instrumented RelaSharp code is entirely mechanical, this table summarizes the constructs available:

| C# class      | RelaSharp class|Example member         |
|:------------- |:---------------|:----------------------|
| Volatile      | RVolatile      |RVolatile.Read         |
| Interlocked   | RInterlocked   |RInterlocked.Increment |
| Monitor       | RMonitor       |RMonitor.Pulse         |
| N/A           | RUnordered     |RUnordered.Write       |

This obviously omits a number of the blocking constructs available in C# (such as reader-writer locks), but since RelaSharp is focused on the exploring lock-free code, I haven't added these.

## Scheduling

A RelaSharp test supplies the entry point for each of its threads, here is the interface you need to implement to write a test:

```csharp
interface IRelaTest 
{
    IReadOnlyList<Action> ThreadEntries { get; }
    void OnBegin();
    void OnFinished();
}
```

It then creates a thread for each of the functions in ThreadEntries. Most importantly, it then takes over the scheduling of these threads. It does this by initially making all the threads wait on their own condition variable, and then waking the one chosen by the so-called scheduling strategy (which can be either random or exhaustive). Every time an instrumented instruction is executed, for example RVolatile.Read, a call is made to the scheduler. This gives it the opportunity to pre-empt the running thread by waking another thread and blocking the currently running thread.

### Random Scheduling

The simple, most practical and default way of using RelaSharp is with the random scheduler. The random scheduler uniformly at random chooses the next thread to run at each instrumented instruction. This generally much more aggressive than running with an OS scheduler. The random scheduler typically finds subtle memory-ordering issues in a matter of seconds. 

### Exhaustive Scheduling

RelaSharp also supports exhaustively exploring all thread interleavings of a given test, using the so-called exhaustive scheduler. This is often intractable for all but very small test cases. The exhaustive scheduler performs a depth-first search of the thread interleavings. Doing this requires recording the scheduling choice made at each pre-emption point into a "choice history", and iteratively tweaking the choice history to explore all interleavings.

The exhaustive scheduler is much more complicated than the random scheduler due to the need for it to enforce _fairness_. This is best explained with a small example

```csharp
void ReleaseThread()
{
    _x.Store(23, MemoryOrder.Relaxed);
    _x.Store(2, MemoryOrder.Relaxed);
    _flag.Store(0, MemoryOrder.Relaxed);
    _flag.Store(1, MemoryOrder.Release);
}

void AcquireThread()
{
    while(_flag.Load(MemoryOrder.Acquire) == 0) // Spin
    {
        continue;
    }
    int result = _x.Load(MemoryOrder.Relaxed);
    TE.Assert(result == 2, $"Expected to load 2 into result, but loaded {result} instead!");
}
```
The problem this code presents to an exhaustive scheduler is the spin loop. A naive depth-first search of thread interleavings won't work here: the spin loop will create an infinitely long execution when it is encountered over and over again. The scheduler is said to "diverge". To prevent this, we need to provide a hint to the scheduler to give it an opportunity to break the cycle. In RelaSharp, this is done with a yield:

```csharp
void AcquireThread()
{
    while(_flag.Load(MemoryOrder.Acquire) == 0) // Spin
    {
        TE.Yield(); // Tell the exhaustive scheduler it's OK to choose another thread.
    }
    int result = _x.Load(MemoryOrder.Relaxed);
    TE.Assert(result == 2, $"Expected to load 2 into result, but loaded {result} instead!");
}
``` 
The exhaustive scheduler that RelaSharp implements is closely based on the so-called "fair, demonic" scheduler used in CHESS.
Of course it is still possible for the exhaustive scheduler to correctly diverge when a genuine [live-lock](Examples/LiveLock.cs) exists.

## Limitations

RelaSharp's most obvious limitations are described below, these could be lifted with a little more work.

### Manual instrumentation 

Although converting a blocking or lock-free algorithm to a RelaSharp test case is entirely mechanical, it is still annoying
as it essentially boils down to keeping two copies of source code. 

### Execution-order restriction 

Although not of great interest for C#, RelaSharp can only simulate memory re-orderings that result from loads seeing values from previous (in execution order) stores.

For example, consider the following code:

```csharp
// Assume all operations are relaxed.
void Thread1()
{
    a = x; 
    y = 16;
}
void Thread2()
{
    b = y; 
    x = 16;
}
```
At termination, without any compiler optimizations, on POWER and ARM, it is possible to observe a == b == 16. As a result, this execution is legal in the C++11 memory model. RelaSharp currently can't simulate such executions since it involves loads seeing the results of future stores.

## Possible Enhancements

### Automatic instrumentation 

The manual instrumentation of code is the main annoyance in RelaSharp. This would be reasonably straightforward requirement to lift I think. There is an (undocumented) library for the CLR that I'll refer to as the [extended reflection] API, that supports calls like this 

```csharp
TODO NICK
```
As well as automatically replacing the Interlocked, Volatile as shown, it would also be necessary perform some GC pinning. I think the only instrumentation overhead this would leave would be the use of RUnordered. 

### Support remaining C\# threading constructs

It'd obviously be nice to support the remaining blocking C# threading constructs, like Mutex, ReaderWriterLock, ReaderWriterLockSlim, etc.
This would be a for the most part straightforward but not terribly exciting bit of implementation. I mainly haven't done this as I suspect I wouldn't learn all that much from it.

### Context bounding

CHESS introduced a neat idea called _context bound scheduling_ to balance between an exhaustive scheduling of threads and the number of thread interleavings explored. Relacy also implements this idea, that originates with CHESS. Context bound scheduling is the same as an exhaustive scheduler but requires a positive integer parameter called the _context bound_. Each time the scheduler pre-empts a thread at a point when the thread could continue without blocking it decrements the context bound. When the context bound reaches zero, no more pre-emptions are performed. The efficacy of context-bounding is based on the empirical claim that most threading bugs can be found with a fairly small context bound.

Implementing context bounding in RelaSharp would be a very small job requiring only a small tweak to the exhaustive scheduler.

### Parallelization

The exploration that RelaSharp performs of memory re-orderings and thread schedulings is trivially parallelizable when the random scheduler is in use. This again would be a small implementation effort, requiring only that several TestEnvironments are created, and some small assumptions around static variables removed.

### PCT Scheduling

Probabalistic Concurrency Testing (PCT) scheduling aims to operate in a similar fashion to context bounding: it uses an integer parameter called _bug depth_ to limit its search. It seems to be attractive because it's simpler to implement and higher performance than an exhaustive scheduler with context bounding: it operates more like a simple randomized scheduler which is more disciplined in its choice of randomization. Essentially, PCT scheduling assigns threads a strict priority order and then adjusts thread priorities at randomly selected execution points. This allows it to explore the search space of schedulings according to what it defines as the number of _scheduling constraints_ (or bug depth). 

I think this is a medium sized job to implement, selecting the random points to insert priority-inversions would require a little thought. I've not felt a great need for it as I've found the simple random scheduler quite effective at finding even subtle bugs.

### DPOR

For the exhaustive scheduler, so-called Dynamic Partial Order Reduction could be performed to reduce the size of the search space substantially. I've not really explored this so I'm not sure how much work it would be to get most of the benefit from the technique.

### Thread Replay Optimizations

When RelaSharp executes a test case it records an event-log of all decisions it makes so that they can be presented to the user in the event of a test failure. Relacy performs a nice optimization for event-logging: it records only a minimal sequence of scheduling decisions for each test but no event log. When a failure is encountered it runs the test one more time with full event-logging enabled. I'm not sure how much of a difference this would make to performance, but it'd be easy to get a rough idea by disabling event logging altogether and looking at the difference in number of test iterations per second.

### Promises / Load Speculation

Lifting the execution-order restriction of RelaSharp could be done using a technique I'll call _promises_ pioneered in a tool called CDSChecker. 
Implementing promises would add a fair bit of complexity to RelaSharp. The way promises work is that values written by stores are recorded in a set called _futureValues_. As a test is repeatedly executed a given load's history consists not only of the values written by stores that preceed it in execution order, but also those in futureValues. When load is answered with a value chosen from futureValues, some care is required. A store must then be identified that _satisfies_ the load. If one cannot be found, the "speculation" has failed and the test iteration must be aborted. Using promises, the example described above in the "Execution-order restriction" section can produce a == b == 16 as a possible execution.

I think this would be reasonably complicated to implement, but perhaps not too bad for the random scheduler. I write lock-free code mostly for the very forgiving CLR on x86 - if one day I have to use something with a more relaxed memory model I might be forced to implement this!

## Related Tools

RelaSharp is heavily based on tools that precede it, and which have more features than it. Some of these tools are C++ specific, however.

### Relacy

Relacy is an _amazing_ piece of software. Relacy requires manually instrumented C++ code. The instrumentation is fairly painless as it really just requires replacing C++'s std::atomic with Relacy's version. For exhaustive scheduling, Relacy requires scheduler hints. These are like the yield of CHESS (which I also implemented in RelaSharp), but quite different in the details. Relacy has several different forms of yield, e.g. linear and exponential scheduler back-off for the yielding thread.

Relacy has an extremely rich feature set, two nice examples are ABA detection in dynamic memory allocation and spurious CAS failures.
Relacy even supports simulating the Java and CLR memory models, albeit, 

Relacy's only real missing feature is that it is restricted to execution-order only memory re-orderings, and does not implement promises.

### CDSChecker

### CHESS

### SPIN/Promela, TLA+

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


