using System;

namespace RelaSharp
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

        public void Store(T data, ShadowThread runningThread, Action failTest)
        {
            if(_loadClock.AnyGreater(runningThread.VC) || _storeClock.AnyGreater(runningThread.VC))
            {
                failTest();
                return;
            }
            runningThread.IncrementClock();
            _storeClock[runningThread.Id] = runningThread.Clock;
            _data = data;
            return;
        }

        public T Load(ShadowThread runningThread, Action failTest)
        {
            if(_storeClock.AnyGreater(runningThread.VC))  
            {
                failTest();
                return default(T);
            }
            runningThread.IncrementClock();
            _loadClock[runningThread.Id] = runningThread.Clock;
            return _data;
        }
    }    
}