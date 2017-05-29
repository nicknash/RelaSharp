using System;

namespace RelaSharp.MemoryModel
{
    class InternalRaceChecked<T>
    {
        private T _data;
        private VectorClock _loadClock;
        private VectorClock _storeClock;

        public InternalRaceChecked(int numThreads)
        {
            _loadClock = new VectorClock(numThreads);
            _storeClock = new VectorClock(numThreads);
        }

        public void Store(T data, ShadowThread runningThread, Action<string> failTest)
        {
            if(_loadClock.AnyGreater(runningThread.ReleasesAcquired) || _storeClock.AnyGreater(runningThread.ReleasesAcquired))
            {
                failTest($"Data race detected in store on thread {runningThread.Id} @ {runningThread.Clock}");
                return;
            }
            runningThread.IncrementClock();
            _storeClock[runningThread.Id] = runningThread.Clock;
            _data = data;
            return;
        }

        public T Load(ShadowThread runningThread, Action<string> failTest)
        {
            if(_storeClock.AnyGreater(runningThread.ReleasesAcquired))  
            {
                failTest($"Data race detected in load on thread {runningThread.Id} @ {runningThread.Clock}");
                return default(T);
            }
            runningThread.IncrementClock();
            _loadClock[runningThread.Id] = runningThread.Clock;
            return _data;
        }
    }    
}