using System;
using System.Threading;

namespace ConsoleApplication
{
/*
    interface ITest 
    {
        IReadOnlyList<Action> Threads { get; }
        void Setup();
    }
*/

    enum MemoryOrder
    {
        Acq,
        Rel,
        AcqRel,
        SeqCst
    }

    class FullyChecked<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private T _data;
        //private TrackingData trackingData;

        public FullyChecked(T data)
        {
            //_trackingData = TestEnvironment.TE.NewFullyChecked();
        }

        public void Store(T data, MemoryOrder mo)
        {
            //TE.scheduler();
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
        private ulong[] _clocks;
        public readonly int Size;
        
        public VectorClock(int size)
        {
            _clocks = new ulong[size];
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
                if(other.clocks[i] >= _clocks[i]) // TODO: revisit >= vs >
                {
                    return false;
                }
            }
            return true;
        }

        public void Join(VectorClock other)
        {

        }

        public void Assign(ulong value)
        {

        }
    }

    // TODO: Need lock(..) wrappers...

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
            _storeClock.Assign(TE.RunningThread.Clock);
            _data = data;
        }

        public T Load()
        {
            if(!_storeClock.IsBefore(TE.RunningThread.VC) // yuck 
            {
                // DATA RACE 
                // because the loading thread has not seen the writes performed by at least one other thread 
            }
            TE.RunningThread.IncrementClock();
            _loadClock.Assign(TE.RunningThread.Clock);
            return _data;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            //var test = new PetersenTest();
            //TestRunner.Run(test);
            TestEnvironment.TE.SetupTest();
            
        }
    }
    
    class ShadowThread
    {
        private long _clock;
        public readonly VectorClock VC; // 

        public void IncrementClock()
        {

        }


    }

    public class TestEnvironment
    {
        public static TestEnvironment TE = new TestEnvironment();
    
        private int _runningThreadIdx;
        private int _numThreads;
        private Thread[] _threads;

        private bool[] _isRunning;
        private Object[] _threadLocks;

        private Random _random = new Random();

        private void GenericThreadFunc(int idx)
        {
            //int iter = 0;
            while(true)
            {
                //Console.WriteLine($"Thread-{idx}, run number: {++iter}");
                Console.WriteLine(idx);
                //Thread.Sleep(500);
                Scheduler();
            }
        }

        private void MakeThreadFunction(Action threadFunction, int threadIdx)
        {
            var l = _threadLocks[threadIdx]; 
            lock(l)
            {
                Monitor.Wait(l);
            }
            threadFunction();
        }



        public void SetupTest()
        {
            _numThreads = 5;
            _threads = new Thread[_numThreads];
            _isRunning = new bool[_numThreads];
            _threadLocks = new Object[_numThreads];
            for(int i = 0; i < _numThreads; ++i)
            {
                _threadLocks[i] = new Object();
                int j = i;
                Action threadFunc = () => GenericThreadFunc(j);
                Action wrapped = () => MakeThreadFunction(threadFunc, j); 
                _threads[i] = new Thread(new ThreadStart(wrapped));
                _threads[i].Start();
            }
            Thread.Sleep(100); // TODO: Sync. properly with all threads going to sleep. Prevent a hang if thread 0 not asleep yet!
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

/*
    public class PetersenTest : IThread 
    {
        private MemOrdered<int> flag0;
        private MemOrdered<int> flag1;
        private MemOrdered<int> turn;


        private List<Action> _threads = new List<Action>{Thread1, Thread2};
        public IReadOnlyList<Action> Threads => _threads;

        IEnumerable<int> Thread1()
        {
            flag0.Store(25);
            yield return 0;
            flag1.Store(35);

        }

        void Thread2()
        {

        }


        void Setup()
        {
        //     flag0 = 0;
        //     flag1 = 0;
        //     turn = 0;
        }

        void RunThread(int index)
        {
            if(index == 0)
            {

            }
        }
    }

    public struct MemOrdered<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private T _data;
        private TrackingData trackingData;

        public MemOrdered<T>(T data)
        {
            _trackingData = TestEnvironment.TE.newMemOrdered()
        
        }

        public void Store()
        {
            TE.scheduler();
        }
    }
    */

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
    }

    public class TrackingData
    {

    }
*/

}
