using System;

namespace RelaSharp
{
    class RandomScheduler // TestEnvironment/TestRunner calls the scheduler.
    {
        private Random _random = new Random();
        private int _runningThreadIdx;
        private int[] _unfinishedThreadIds;
        private int[] _waitingThreadIds;
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
            RunningThreadId = MaybeSwitch();
        }

        // Maybe GetNextThreadId is the more natural interface to support?

        public int MaybeSwitch()
        {
            if(_numUnfinishedThreads == 0)
            {
                throw new Exception("All threads finished. Who called?");
            }
            RunningThreadId = _random.Next(_numUnfinishedThreads);
            return _unfinishedThreadIds[RunningThreadId];
        }

        public bool ThreadWaiting()
        {
            return false; /// TODO return true if deadlock.
        } 

        public void ThreadFinished(int threadId)
        {
            // TODO call _scheduler for this.
            if(_numUnfinishedThreads > 1)
            {
                int i = Array.IndexOf(_unfinishedThreadIds, threadId);
                _unfinishedThreadIds[i] = _unfinishedThreadIds[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
            }
        }
    }

}