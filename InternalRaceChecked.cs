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
            if(!_loadClock.IsBefore(runningThread.VC))
            {
                // DATA RACE 
                // The storing thread has not seen a load by at least one other thread!
            }
            if(!_storeClock.IsBefore(runningThread.VC))
            {
                // DATA RACE 
                // The storing thread has not seen a store by at least one other thread!
            }
            runningThread.IncrementClock();
            _storeClock[runningThread.Id] = runningThread.Clock;
            _data = data;
        }

        public T Load(ShadowThread runningThread)
        {
            if(!_storeClock.IsBefore(runningThread.VC))  
            {
                // DATA RACE 
                // because the loading thread has not seen the writes performed by at least one other thread 
            }
            runningThread.IncrementClock();
            _loadClock[runningThread.Id] = runningThread.Clock;
            return _data;
        }
    }    
}