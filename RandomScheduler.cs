using System;

namespace RelaSharp
{
    class RandomScheduler // TestEnvironment/TestRunner calls the scheduler.
    {
        private Random _random = new Random();
        private int[] _unfinishedThreadIds;
        private int[] _waitingThreadIds;
        private int _numWaitingThreads;
        private int _numUnfinishedThreads; // TODO: wrap these two into a proper data structure when shape clearer.

        public bool AllThreadsFinished => _numUnfinishedThreads == 0;

        public int RunningThreadId { get; private set; }

        public RandomScheduler(int numThreads)
        {
            _unfinishedThreadIds = new int[numThreads];
            _numUnfinishedThreads = numThreads;
            for(int i = 0; i < numThreads; ++i)
            {
                _unfinishedThreadIds[i] = i;
            }
            MaybeSwitch();
        }

        public int MaybeSwitch()
        {
            if(_numUnfinishedThreads == 0)
            {
                throw new Exception("All threads finished. Who called?");
            }
            var idx = _random.Next(_numUnfinishedThreads);
            RunningThreadId =_unfinishedThreadIds[idx]; 
            return RunningThreadId;
        }

        public bool ThreadWaiting()
        {
            if(Array.IndexOf(_waitingThreadIds, RunningThreadId) == -1)
            {
                _waitingThreadIds[_numWaitingThreads] = RunningThreadId;
                _numWaitingThreads++;
            }
            bool deadlock = _numWaitingThreads == _numUnfinishedThreads;
            return deadlock;
        } 

        public void ThreadFinished()
        {
            if(_numUnfinishedThreads > 0)
            {
                int i = Array.IndexOf(_unfinishedThreadIds, RunningThreadId);
                _unfinishedThreadIds[i] = _unfinishedThreadIds[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
            }
        }
    }

}