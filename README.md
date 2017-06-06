# RelaSharp

## Overview

RelaSharp is a tool I developed for verifying lock and wait-free algorithms in C#. It is inspired by Relacy (hence the name RelaSharp) and CHESS.
I mostly wrote it in order to get a better feel for memory models in general, but it is also genuinely useful for vastly increasing
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

## Demonstration Tool

TODO


## Features

Roughly the things that RelaSharp does is:

* Generate executions consistent with the C++11 memory model, except consume semantics.
* Dead-lock detection (including lost thread wake-ups), when exclusive locks are in use.
* Live-lock detection: When a very long execution is generated, it is heuristically classified as a live-lock.
* Random thread scheduling: At every instrumented instruction, possibly generate a pre-emption.
* Exhaustive "fair demonic" scheduling: Depth-first search of all thread interleavings, avoiding false divergence into live-lock.
* Execution tracing: Detailed output showing file/line number and the results of every volatile load/store, interlocked instruction, etc.

## Basic Algorithm

RelaSharp works by wrapping all memory model related instructions in its own types, for example, RInterlocked.CompareExchange 
wraps Interlocked.CompareExchange. This allows it to do three main things:

* Generate pre-emption points for the scheduler
* Generate results for the instruction that are consistent with the execution so far, but more likely to include
  a memory re-ordering than running the code natively would allow. 
* Record file/line number and results of the execution for review if the test fails.

## Memory Model Simulation

## Generic Interface

## C# Specific Interface

## Scheduling

## Limitations

## Possible Enhancements

## Related Tools

## References


