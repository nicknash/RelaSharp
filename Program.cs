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
        public VectorClock LastSeen { get; private set; }
        
        public T Payload { get; private set; }
        public bool IsInitialized { get; private set; }

        public AccessData(int numThreads)
        {
            ReleasesToAcquire = new VectorClock(numThreads);
            LastSeen = new VectorClock(numThreads);
        }

        public void RecordStore(int threadIdx, long threadClock, T payload)
        {
            LastSeen.SetAllClocks(VectorClock.BeforeAllTimes);
            LastSeen[threadIdx] = threadClock;
            LastStoredThreadId = threadIdx;
            LastStoredThreadClock = threadClock;
            IsInitialized = true;
            Payload = payload;
        }

        public void RecordLoad(int threadIdx, long threadClock)
        {
            LastSeen[threadIdx] = threadClock;
        }
    }

    class AccessDataPool<T>
    {
        public int CurrentIndex { get; private set; }
        public int SizeOccupied { get; private set; }
        private AccessData<T>[] _pool;

        public AccessDataPool(int length, int numThreads)
        {
            _pool = new AccessData<T>[length];
            for(int i = 0; i < length; ++i)
            {
                _pool[i] = new AccessData<T>(numThreads);
            }
        }

        public AccessData<T> GetNext()
        {
            CurrentIndex++;
            if(SizeOccupied < _pool.Length)
            {
                SizeOccupied = CurrentIndex;
            }
            return this[CurrentIndex];
        }

        public AccessData<T> this[int idx]
        {
            get
            {
                int wrapped = idx % _pool.Length;
                if(wrapped < 0)
                {
                    wrapped += _pool.Length;
                }
                return _pool[wrapped];
            }
        }
    } 

    class AccessHistory<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE; // TODO: remove, ultimately pass this around as shallowly as possible 

        private readonly AccessDataPool<T> _history;
        private readonly int _numThreads;
        private readonly Random _random;

        public AccessHistory(int length, int numThreads)
        {
            _history = new AccessDataPool<T>(length, numThreads);
            _numThreads = numThreads;
            _random = new Random();
        }
        
        public void RecordStore(MemoryOrder mo, T data)
        {
            var runningThread = TE.RunningThread;
            var storeTarget = _history.GetNext();
            storeTarget.RecordStore(runningThread.Id, runningThread.Clock, data);

            bool isAtLeastRelease = mo == MemoryOrder.Release || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            
            var sourceClock = isAtLeastRelease ? TE.RunningThread.VC : TE.RunningThread.Fenced;
            var previous = _history[_history.CurrentIndex - 1];
            bool isReleaseSequence = previous.IsInitialized && previous.LastStoredThreadId == TE.RunningThread.Id; // TODO: OR this is part of a read-modify-write
            if(isReleaseSequence)
            {
                storeTarget.ReleasesToAcquire.Assign(previous.ReleasesToAcquire);
                storeTarget.ReleasesToAcquire.Join(sourceClock);
            }
            else
            {
                storeTarget.ReleasesToAcquire.Assign(sourceClock);
            }
        }

        public T RecordPossibleLoad(MemoryOrder mo)
        {
            var runningThread = TE.RunningThread;
            var loadData = GetPossibleLoad(runningThread.VC, runningThread.Id);
            loadData.RecordLoad(runningThread.Id, runningThread.Clock);
            bool isAtLeastAcquire = mo == MemoryOrder.Acquire || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            var destinationClock = isAtLeastAcquire ? runningThread.VC : runningThread.Fenced; // TODO: AcquireFenced
            destinationClock.Join(loadData.ReleasesToAcquire);  
            return loadData.Payload;
        }

        private AccessData<T> GetPossibleLoad(VectorClock releasesAcquired, int threadId)
        {
            int j = _history.CurrentIndex;
            int lookBack = _history.SizeOccupied;//_random.Next(_history.SizeOccupied);
            for(int i = 0; i < lookBack; ++i)
            {
                if(!_history[j].IsInitialized)
                {
                    // TODO: Replace with add to event log, throw exception
                    // Perhaps can be made never to happen.
                    Console.WriteLine("ACCESS TO UNINITIALIZED VARIABLE");
                }
                var accessData = _history[j];
                // Has the loading thread synchronized-with a later release of the last storing thread to this variable?
                if(releasesAcquired[accessData.LastStoredThreadId] >= accessData.LastStoredThreadClock)
                {
                    Console.WriteLine($"STOPPING: {releasesAcquired[accessData.LastStoredThreadId]} >= {accessData.LastStoredThreadClock}");
                    // If so, this is the oldest load that can be returned, since this thread has synchronized-with 
                    // ("acquired a release" of) the storing thread at or after this store.
                    break;
                }
                    Console.WriteLine($"GOING BACK! {releasesAcquired[accessData.LastStoredThreadId]} < {accessData.LastStoredThreadClock}");
                
                // Has the loading thread synchronized-with any thread that has loaded a later value 
                // of this variable?
                if(!releasesAcquired.IsAtOrBefore(accessData.LastSeen))
                {
                    // If so, this is the oldest load that can be returned to the loading thread, otherwise it'd
                    // be going back in time, since it has synchronized-with ("acquired a release" of) a thread that has seen a later value 
                    // of this variable.
                    break;
                }
                --j;
            }
            return _history[j];
        }

    }

    class MemoryOrdered<T> // TODO: restrict to atomic types.
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private InternalMemoryOrdered<T> _memoryOrdered;

        private void MaybeInit()
        {
            if(_memoryOrdered == null)
            {
                _memoryOrdered = new InternalMemoryOrdered<T>(TE.HistoryLength, TE.NumThreads);
            }
        }

        public void Store(T data, MemoryOrder mo)
        {
            MaybeInit();
            TE.Scheduler();
            TE.RunningThread.IncrementClock();
            _memoryOrdered.Store(data, mo);
        }

        public T Load(MemoryOrder mo)
        {
            MaybeInit();
            TE.Scheduler();
            TE.RunningThread.IncrementClock();
            return _memoryOrdered.Load(mo);
        }
    }

    class InternalMemoryOrdered<T> 
    {
        private AccessHistory<T> _history;

        public InternalMemoryOrdered(int historyLength, int numThreads)
        {
            _history = new AccessHistory<T>(historyLength, numThreads);
        }

        public void Store(T data, MemoryOrder mo)
        {
            _history.RecordStore(mo, data);
        }

        public T Load(MemoryOrder mo)
        {
            T result = _history.RecordPossibleLoad(mo);
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

        private void CheckSize(VectorClock other)
        {
            if(Size != other.Size)
            {
                throw new Exception($"Cannot compare vector clocks of different sizes, this size = {Size}, other size = {other.Size}");
            }
        }

        // Are all clocks in other smaller or equal to this?
        public bool IsAtOrBefore(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(_clocks[i] >= other._clocks[i])
                {
                    return false;
                }
            }
            // i.e., _clocks[i] < other._clocks[i] for all i
            return true;
          
        }

        // Are all clocks in other larger or equal to this?
        public bool IsNotAfter(VectorClock other) 
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(other._clocks[i] >= _clocks[i])
                {
                    return false;
                }
            }
            return true;
        }

        public long this[int idx]
        {
            get
            {
                return _clocks[idx];
            }
            set
            {
                _clocks[idx] = value;
            }

        }

        public void SetAllClocks(long v)
        {
            for(int i = 0; i < Size; ++i)
            {
                _clocks[i] = v;
            }
        }

        public void Join(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(other._clocks[i] > _clocks[i])
                {
                    _clocks[i] = other._clocks[i];
                }
            }
        }

        public void Assign(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                _clocks[i] = other._clocks[i];
            }
        }
    }

    // TODO: Need lock(..), fence, cmp exch, wrappers...
/*
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
  */  
    class ShadowThread
    {
        public long Clock => VC[Id];
        
        public readonly VectorClock VC; // TODO: Think about better names for these. "ReleasesAcquired" ?
        public readonly VectorClock Fenced;  
        public readonly int Id;

        public ShadowThread(int id, int numThreads)
        {
            Id = id;
            VC = new VectorClock(numThreads);
            Fenced = new VectorClock(numThreads);
        }

        public void IncrementClock()
        {
            VC[Id]++;
            //Console.WriteLine($"Thread {Id} clock is {Clock}");
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

        public ShadowThread RunningThread => _shadowThreads[_runningThreadIdx];
        public int NumThreads { get; private set; }
        public int HistoryLength => 20;

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
            ThreadEntries = new List<Action>{Thread1,Thread2};
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
        }

        private void Thread1()
        {
            //while(true)
            for(int i = 0; i < 10; ++i)
            {
                //Console.WriteLine($"Storing {i}");
                //flag0.Load(i, MemoryOrder.Acquire);
                Console.WriteLine($"Loaded {flag0.Load(MemoryOrder.Acquire)}");
            }
        }

        private void Thread2()
        {
            //while(true)
            for(int i = 0; i < 5; ++i)
            {
                Console.WriteLine($"Storing {i}");
                flag0.Store(i, MemoryOrder.Release);
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

}
