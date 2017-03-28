using System;
using System.Threading;

namespace RelaSharp
{
    enum ThreadState 
    {
        Running,
        Blocked,
        Finished
    }
    
    class TestFailedException : Exception 
    {

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

        private bool _testFailed;

        private void MakeThreadFunction(Action threadFunction, int threadIdx)
        {
            var l = _threadLocks[threadIdx]; 
            
            lock(l)
            {
                _threadStates[threadIdx] = ThreadState.Blocked;
                Monitor.Pulse(l);                
                Monitor.Wait(l);
            }
            try
            {
                threadFunction();
            }
            catch(TestFailedException e)
            {
                Console.WriteLine($"test failed on thread {threadIdx}");
            }
            _threadStates[threadIdx] = ThreadState.Finished;
            if(_numUnfinishedThreads > 1)
            {
                int i = Array.IndexOf(_unfinishedThreadIndices, threadIdx);
                _unfinishedThreadIndices[i] = _unfinishedThreadIndices[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
                int nextIdx = GetNextThreadIdx(); 
                WakeThread(nextIdx);
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
            for(int i = 0; i < NumThreads; ++i)
            {
                _threads[i].Join();
            }
            test.OnFinished();
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
            if(_testFailed)
            {
                throw new TestFailedException();
            }
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

        public void FailTest()
        {
            _testFailed = true;
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
}