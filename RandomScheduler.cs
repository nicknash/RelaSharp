using System;

namespace RelaSharp
{
    class RandomScheduler
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
            _waitingThreadIds = new int[numThreads];
            _numUnfinishedThreads = numThreads;
            for(int i = 0; i < numThreads; ++i)
            {
                _waitingThreadIds[i] = -1;
                _unfinishedThreadIds[i] = i;
            }
            MaybeSwitch();
        }

        public void MaybeSwitch()
        {
            if(_numUnfinishedThreads == 0)
            {
                throw new Exception("All threads finished. Who called?");
            }
            var idx = _random.Next(_numUnfinishedThreads);
            RunningThreadId =_unfinishedThreadIds[idx]; 
            return;
        }

        public bool ThreadWaiting()
        {
            if(Array.IndexOf(_waitingThreadIds, RunningThreadId) == -1)
            {
                _waitingThreadIds[_numWaitingThreads] = RunningThreadId;
                _numWaitingThreads++;
            }
            bool deadlock = _numWaitingThreads == _numUnfinishedThreads;
            int originalId = RunningThreadId;
            while(RunningThreadId == originalId) 
            {
                MaybeSwitch();
            }
            return deadlock;
        } 

        public void ThreadFinishedWaiting()
        {
            int idx = Array.IndexOf(_waitingThreadIds, RunningThreadId);
            _numWaitingThreads--;
            _waitingThreadIds[idx] = _waitingThreadIds[_numWaitingThreads];  
        }

        public void ThreadFinished()
        {
            if(_numUnfinishedThreads > 0) // Why check this and not fail loudly?
            {
                int i = Array.IndexOf(_unfinishedThreadIds, RunningThreadId);
                _unfinishedThreadIds[i] = _unfinishedThreadIds[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
            }
        }
    }
}