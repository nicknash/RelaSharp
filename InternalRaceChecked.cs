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

        public void Store(T data, ShadowThread runningThread)
        {
            if(_loadClock.AnyGreater(runningThread.VC))
            {
                // DATA RACE 
            }
            if(_storeClock.AnyGreater(runningThread.VC))
            {
                // DATA RACE 
            }
            runningThread.IncrementClock();
            _storeClock[runningThread.Id] = runningThread.Clock;
            _data = data;
        }

        public T Load(ShadowThread runningThread)
        {
            if(_storeClock.AnyGreater(runningThread.VC))  
            {
                // DATA RACE 
            }
            runningThread.IncrementClock();
            _loadClock[runningThread.Id] = runningThread.Clock;
            return _data;
        }
    }    
}