﻿using System;
using System.Threading;
using System.Collections.Generic;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var test = new PetersenTest();
            TestEnvironment.TE.RunTest(test);
            
            //TestRunner.Run(test);
            //TestEnvironment.TE.SetupTest();
            
        }
    }

    interface ITest 
    {
        IReadOnlyList<Action> ThreadEntries { get; }
    }


    enum MemoryOrder
    {
        Relaxed,
        Acquire,
        Release,
        AcquireRelease,
        SequentiallyConsistent
    }

    class AccessData<T> // Not sure if encapsulation provided by this type is desirable or right structure. 
    {
        public int LastStoredThreadId { get; private set; }
        public long LastStoredThreadClock { get; private set; }
        public VectorClock ReleasesToAcquire { get; private set; }
        
        public T Payload { get; private set; }
        public bool IsInitialized { get; private set; }

        private VectorClock _lastSeen;

        public AccessData(int numThreads)
        {
            _lastSeen = new VectorClock(numThreads);
            ReleasesToAcquire = new VectorClock(numThreads);
        }

        public void RecordStore(int threadIdx, long threadClock, T payload)
        {
            _lastSeen.SetAllClocks(VectorClock.BeforeAllTimes);
            _lastSeen.SetClock(threadIdx, threadClock);
            LastStoredThreadId = threadIdx;
            LastStoredThreadClock = threadClock;
            IsInitialized = true;
            Payload = payload;
        }

        public void RecordLoad(int threadIdx, long threadClock)
        {
            _lastSeen.SetClock(threadIdx, threadClock);
        }
    }

    class AccessDataPool<T>
    {
        public int CurrentIndex { get; private set; }
        private int _length;
        private AccessData<T>[] _pool;

        public AccessDataPool(int length, int numThreads)
        {
            _length = length;
            _pool = new AccessData<T>[length];
            for(int i = 0; i < length; ++i)
            {
                _pool[i] = new AccessData<T>(numThreads);
            }
        }

        public AccessData<T> GetNext()
        {
            CurrentIndex = (CurrentIndex + 1) % length;
            return _pool[CurrentIndex]; // N.B. index-1 returned first
        }

        public AccessData<T> this[int idx]
        {
            get
            {
                return _pool[idx];
            }
        }
    } 

    class AccessHistory<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE; // TODO: remove, ultimately pass this around as shallowly as possible 

        private AccessDataPool<T> _history;


        public AccessHistory(int length, int numThreads)
        {
            _history = new AccessDataPool(length, numThreads);
        }
        
        public void RecordStore(T data)
        {
            var runningThread = TE.RunningThread;
            var storeTarget = _pool.GetNext();
            storeTarget.RecordStore(runningThread.Id, runningThread.Clock, data);

            bool isAtLeastRelease = mo == MemoryOrder.Release || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            var sourceClock = isAtLeastRelease ? TE.RunningThread.VC : TE.RunningThread.Fenced;
 
            bool isReleaseSequence = false;//previousRecord.LastStoredThreadId == TE.RunningThread.Id; // || this is part of a RMW
            if(isReleaseSequence)
            {
                // TODO
            }
            else
            {
            
            }
        }

        public T RecordPossibleLoad(MemoryOrder mo)
        {
            var loadData = GetPossibleLoad();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            loadData.RecordLoad(runningThread.Id, runningThread.Clock);
            bool isAtLeastAcquire = mo == MemoryOrder.Acquire || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            var destinationClock = isAtLeastAcquire ? runningThread.VC : runningThread.Fenced;
            destinationClock.Join(loadData.ReleasesToAcquire);  
            return loadData.Data;
        }

        private AccessData<T> GetPossibleLoad()
        {
            return _history[0];
        }

        private int GetIndex()
        {

        }
    }

    class MemoryOrdered<T> // TODO: restrict to atomic types.
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private AccessHistory<T> _history;

        public MemoryOrdered()
        {
            _history = new AccessHistory<T>();
        }

        public void Store(T data, MemoryOrder mo)
        {
            TE.Scheduler();
            TE.RunningThread.IncrementClock();
            _history.RecordStore(mo, data);
        }

        public T Load(MemoryOrder mo)
        {
            TE.Scheduler();
            T result = _history.RecordPossibleLoad(mo);
            TE.RunningThead.IncrementClock();
            return result;
        }

        

        // Atomics ...
    }

    class VectorClock
    {
        public const long BeforeAllTimes = -1;
        private long[] _clocks;
        public readonly int Size;
        
        public VectorClock(int size)
        {
            _clocks = new long[size];
            Size = size;
        }

        public bool IsBefore(VectorClock other)
        {
            if(Size != other.Size)
            {
                throw new Exception($"Cannot compare vector clocks of different sizes, this size = {Size}, other size = {other.Size}");
            }
            for(int i = 0; i < Size; ++i)
            {
                if(other._clocks[i] >= _clocks[i]) // TODO: revisit >= vs >
                {
                    return false;
                }
            }
            return true;
        }

        public void SetClock(int idx, long v)
        {
            _clocks[idx] = v;
        }

        public void SetAllClocks(long v)
        {

        }

        public void Join(VectorClock other)
        {

        }

        public void Assign(VectorClock other)
        {

        }
    }

    // TODO: Need lock(..), fence, cmp exch, wrappers...

    class RaceChecked<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private T _data;
        //private TrackingData trackingData;

        private VectorClock _loadClock;
        private VectorClock _storeClock;

        public RaceChecked(T data)
        {
            //_trackingData = TestEnvironment.TE.NewRaceChecked();
        
        }

        public void Store(T data)
        {
            if(!_loadClock.IsBefore(TE.RunningThread.VC))
            {
                // DATA RACE 
                // The storing thread has not seen a load by at least one other thread!
            }
            if(!_storeClock.IsBefore(TE.RunningThread.VC))
            {
                // DATA RACE 
                // The storing thread has not seen a store by at least one other thread!
            }
            TE.RunningThread.IncrementClock();
            _storeClock.SetClock(TE.RunningThread.Id, TE.RunningThread.Clock);
            _data = data;
        }

        public T Load()
        {
            if(!_storeClock.IsBefore(TE.RunningThread.VC)) // yuck 
            {
                // DATA RACE 
                // because the loading thread has not seen the writes performed by at least one other thread 
            }
            TE.RunningThread.IncrementClock();
            _loadClock.SetClock(TE.RunningThread.Id, TE.RunningThread.Clock);
            return _data;
        }
    }
    
    class ShadowThread
    {
        public long Clock { get; private set; } // TODO: wrap "clock" in timestamp type.
        
        public readonly VectorClock VC; // TODO: Think about better names for these. "ReleasesAcquired" ?
        public readonly VectorClock Fenced;  
        public readonly int Id;

        public ShadowThread(int id, int numThreads)
        {
            Id = id;
            VC = new VectorClock(numThreads);
        }

        public void IncrementClock()
        {

        }
    }

    enum ThreadState 
    {
        Running,
        Blocked,
        Finished
    }

    class TestEnvironment
    {
        public static TestEnvironment TE = new TestEnvironment();

        public ShadowThread RunningThread { get; private set; }
        public int NumThreads { get; private set; }
        public int HistoryLength { get; private set; }

        private int _runningThreadIdx;

        private int[] _unfinishedThreadIndices;
        private int _numUnfinishedThreads; // TODO: wrap these two into a proper data structure when shape clearer.

        private Thread[] _threads;
        private ThreadState[] _threadStates;
        private ShadowThread[] _shadowThreads;
   
        private Object[] _threadLocks;


        private Random _random = new Random();

        private void MakeThreadFunction(Action threadFunction, int threadIdx)
        {
            var l = _threadLocks[threadIdx]; 
            
            lock(l)
            {
                _threadStates[threadIdx] = ThreadState.Blocked;
                Monitor.Pulse(l);                
                Monitor.Wait(l);
            }
            threadFunction();
            _threadStates[threadIdx] = ThreadState.Finished;
            if(_numUnfinishedThreads > 1)
            {
                int i = Array.IndexOf(_unfinishedThreadIndices, threadIdx);
                _unfinishedThreadIndices[i] = _unfinishedThreadIndices[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
                int nextIdx = GetNextThreadIdx(); 
                Console.WriteLine($"Thread {threadIdx} completed. Going to wake: {nextIdx}");
                Console.Out.Flush();            
                WakeThread(nextIdx);
            }
            else
            {
                Console.WriteLine($"I'm the last thread ({threadIdx}) Nobody to wake");
            }
        }

        public void RunTest(ITest test)
        {
            NumThreads = test.ThreadEntries.Count;
            HistoryLength = 20;
            _threads = new Thread[NumThreads];
            _threadStates = new ThreadState[NumThreads];
            _threadLocks = new Object[NumThreads];
            _shadowThreads = new ShadowThread[NumThreads];
            _unfinishedThreadIndices = new int[NumThreads];
            _numUnfinishedThreads = NumThreads;
            for(int i = 0; i < NumThreads; ++i)
            {
                _unfinishedThreadIndices[i] = i;
                _threadLocks[i] = new Object();
                int j = i;
                Action threadEntry = test.ThreadEntries[j];
                Action wrapped = () => MakeThreadFunction(threadEntry, j); 
                _threadStates[i] = ThreadState.Running;
                _threads[i] = new Thread(new ThreadStart(wrapped));
                _threads[i].Start();
                _shadowThreads[i] = new ShadowThread(i, NumThreads);
            }
            for(int i = 0; i < NumThreads; ++i)
            {
                var l = _threadLocks[i];
                lock(l)
                {
                    while(_threadStates[i] == ThreadState.Running)
                    {
                        Monitor.Wait(l);
                    }
                }
            }            
            WakeThread(GetNextThreadIdx());
        }

        private void WakeThread(int idx)
        {
            _runningThreadIdx = idx;
            _threadStates[idx] = ThreadState.Running;
            var l = _threadLocks[idx];
            lock(l)
            {
                Monitor.Pulse(l);
            }
        }

        public void Scheduler()
        {
            int prevThreadIdx = _runningThreadIdx;
            int nextThreadIdx = GetNextThreadIdx();
            if(nextThreadIdx == prevThreadIdx)
            {
                return;
            }
            _threadStates[prevThreadIdx] = ThreadState.Blocked;
            WakeThread(nextThreadIdx);             
            var runningLock = _threadLocks[prevThreadIdx];
            lock(runningLock)
            {
                while(_threadStates[prevThreadIdx] == ThreadState.Blocked) // I may get here, and be woken before I got a chance to sleep. That's OK. 
                {
                    Monitor.Wait(runningLock);
                }

            }        
        }

        private int GetNextThreadIdx()
        {
            if(_numUnfinishedThreads == 0)
            {
                throw new Exception("All threads finished. Who called?");
            }
            return _unfinishedThreadIndices[_random.Next(_numUnfinishedThreads)];
        }
    }


    public class PetersenTest : ITest 
    {
        private MemoryOrdered<int> flag0;
        private MemoryOrdered<int> flag1;
        private MemoryOrdered<int> turn;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}

        public PetersenTest()
        {
            ThreadEntries = new List<Action>{Thread1,Thread2,Thread1,Thread2,Thread1,Thread2};
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
        }

        private void Thread1()
        {
            //while(true)
            for(int i = 0; i < 3; ++i)
            {
                Console.WriteLine("3-Thread");
                flag0.Store(10, MemoryOrder.Release);
            }
        }

        private void Thread2()
        {
            //while(true)
            for(int i = 0; i < 10; ++i)
            {
                Console.WriteLine("10-Thread");
                flag1.Store(20, MemoryOrder.Release);
            }
        }

/*
        void Setup()
        {
        //     flag0 = 0;
        //     flag1 = 0;
        //     turn = 0;
        }*/
    }
    /*
    public class TestRunner 
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        public static void Run(ITest test)
        {
            test.Setup();
            TE.RunningTest = test;
            int runningThread = 0;
            TE.Scheduler();
        }
    }*/

}
