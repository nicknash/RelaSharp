using System;
using System.Threading;
using System.Collections.Generic;

namespace RelaSharp
{
    class TestThreads
    {
        private enum ThreadState
        {
            Running,
            Blocked,
            Waiting,
            Finished
        }

        private Thread[] _threads;
        private ThreadState[] _threadStates;
        private Object[] _threadLocks;
        private object _runningThreadLock = new object();
        private RandomScheduler _scheduler;

        public TestThreads(IRelaTest test, RandomScheduler scheduler)
        {
            var numThreads = test.ThreadEntries.Count;
            _threads = new Thread[numThreads];
            _threadStates = new ThreadState[numThreads];
            _threadLocks = new Object[numThreads];
            _scheduler = scheduler;
            CreateThreads(test.ThreadEntries);
        }

        private void CreateThreads(IReadOnlyList<Action> threadEntries)
        {
            var numThreads = threadEntries.Count;            
            for (int i = 0; i < numThreads; ++i)
            {
                _threadLocks[i] = new Object();
                int j = i;
                Action threadEntry = threadEntries[j];
                Action wrapped = () => MakeThreadFunction(threadEntry, j);
                _threadStates[i] = ThreadState.Running;
                _threads[i] = new Thread(new ThreadStart(wrapped));
                _threads[i].Start();
            }
            for (int i = 0; i < numThreads; ++i)
            {
                var l = _threadLocks[i];
                lock (l)
                {
                    while (_threadStates[i] == ThreadState.Running)
                    {
                        Monitor.Wait(l);
                    }
                }
            }
        }

        public void WakeThread()
        {
            int idx = _scheduler.RunningThreadId;
            _threadStates[idx] = ThreadState.Running;
            var l = _threadLocks[idx];
            lock(l)
            {
                Monitor.Pulse(l);
            }
        }

        public void WakeNewThreadAndBlockPrevious(int previousThreadId)
        {
            _threadStates[previousThreadId] = ThreadState.Blocked;
            WakeThread();             
            var runningLock = _threadLocks[previousThreadId];
            lock(runningLock)
            {
                Monitor.Exit(_runningThreadLock);
                while(_threadStates[previousThreadId] == ThreadState.Blocked)
                {
                    Monitor.Wait(runningLock);
                }
            }
            Monitor.Enter(_runningThreadLock);        
        }

        public void Join()
        {
            for(int i = 0; i < _threads.Length; ++i)
            {
                _threads[i].Join();
            }
        }

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
                Monitor.Enter(_runningThreadLock);
                threadFunction();
            }
            catch(TestFailedException)
            {
            }
            Monitor.Exit(_runningThreadLock);
            _threadStates[threadIdx] = ThreadState.Finished;
            _scheduler.ThreadFinished();
            if(!_scheduler.AllThreadsFinished)
            {
                _scheduler.MaybeSwitch();
                WakeThread();
            }
        }
    }
}