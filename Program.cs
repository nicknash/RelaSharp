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

    class AccessData 
    {
        public int LastStoredThreadId { get; private set; }
        public long LastStoredThreadClock { get; private set; }
        
        private VectorClock _lastSeen;
        private VectorClock _releasesToAcquire;

        public AccessData(int numThreads)
        {
            _lastSeen = new VectorClock(numThreads);
            _releasesToAcquire = new VectorClock(numThreads);
        }

        public void RecordStore(int threadIdx, long threadClock)
        {
            _lastSeen.Assign(VectorClock.BeforeAllTimes);
            _lastSeen.SetClock(threadIdx, threadClock);
            
        }

        public void RecordLoad(int threadIdx, long threadClock)
        {

        }
    }

    class MemoryOrdered<T> // TODO: restrict to atomic types.
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private T _data;
        private AccessData _accessData;
        //private TrackingData trackingData;

        public MemoryOrdered()
        {
            //_trackingData = TestEnvironment.TE.NewFullyChecked();
            _accessData = new AccessData(TestEnvironment.TE.NumThreads);
        }

        public void Store(T data, MemoryOrder mo)
        {
            TE.Scheduler();
            _data = data;
        }

        public T Load(MemoryOrder mo)
        {
            return _data;
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

        public void Join(VectorClock other)
        {

        }

        public void Assign(long value)
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
        
        public readonly VectorClock VC;  
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
        Woken,
        Running,
        Blocked,
        Finished
    }

    class TestEnvironment
    {
        public static TestEnvironment TE = new TestEnvironment();

        public ShadowThread RunningThread { get; private set; }
        public int NumThreads { get; private set; }
    
        private int _runningThreadIdx;
        private int _numThreads;
        private Thread[] _threads;
        private ShadowThread[] _shadowThreads;

        private bool[] _isRunning;
    
        private Object[] _threadLocks;

        private Object _initialThreadLock;

        private Random _random = new Random();

        private void MakeThreadFunction(Action threadFunction, int threadIdx)
        {
            var l = _threadLocks[threadIdx]; 
            lock(l)
            {
                Monitor.Wait(l);
            }
            if(threadIdx == 0)
            {

            }
            threadFunction();
            // TODO: Signal thread exit.
        }

        public void RunTest(ITest test)
        {
            _numThreads = test.ThreadEntries.Count;
            NumThreads = _numThreads;
            _threads = new Thread[_numThreads];
            _isRunning = new bool[_numThreads];
            _threadLocks = new Object[_numThreads];
            _shadowThreads = new ShadowThread[_numThreads];
            _initialThreadLock = new Object();
            for(int i = 0; i < _numThreads; ++i)
            {
                _threadLocks[i] = new Object();
                int j = i;
                Action threadEntry = test.ThreadEntries[j];
                Action wrapped = () => MakeThreadFunction(threadEntry, j); 
                _threads[i] = new Thread(new ThreadStart(wrapped));
                _threads[i].Start();
                _shadowThreads[i] = new ShadowThread(i, _numThreads);
            }
            Thread.Sleep(500); // TODO: Sync. properly with all threads going to sleep. Prevent a hang if thread 0 not asleep yet!
            WakeThread(0);
            Console.WriteLine("startup thread exiting!");
        }

        private void WakeThread(int idx)
        {
            _runningThreadIdx = idx;
            _isRunning[idx] = true;
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
            //Console.Write(nextThreadIdx + " -> ");
            _isRunning[prevThreadIdx] = false;
            WakeThread(nextThreadIdx);             
            var runningLock = _threadLocks[prevThreadIdx];
            lock(runningLock)
            {
                while(!_isRunning[prevThreadIdx]) // I may get here, and be woken before I got a chance to sleep. That's OK. 
                {
                    Monitor.Wait(runningLock);
                }

            }        
        }

        private int GetNextThreadIdx()
        {
            return _random.Next(_numThreads);
            //return (_runningThreadIdx + 1) % _numThreads;
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
            ThreadEntries = new List<Action>{Thread1,Thread2};
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
        }

        private void Thread1()
        {
            while(true)
            {
                Console.WriteLine("PetersenTest Thread1");
                flag0.Store(10, MemoryOrder.Release);
            }
        }

        private void Thread2()
        {
            while(true)
            {
                Console.WriteLine("PetersenTest Thread2");
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
