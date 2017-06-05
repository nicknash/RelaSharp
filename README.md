# RelaSharp

## Overview

RelaSharp is a tool I developed for verifying lock and wait-free algorithms in C#. It is inspired by Relacy and CHESS.
I mostly wrote it in order to get a better feel for memory models in general, but it is also genuinely useful for vastly increasing
confidence in lock-free code, or even exhaustively verifying that the code is correct.

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

TODO, flesh out / make this sensible!
* C++11 memory model, except consume semantics.
* Dead-lock detection
* Live-lock detection
* Random thread scheduling
* Exhaustive "fair demonic" scheduling.
* Execution tracing

## Basic Algorithm

## Memory Model Simulation

## Generic Interface

## C# Specific Interface

## Scheduling

## Limitations

## Possible Enhancements

## Related Tools

## References


