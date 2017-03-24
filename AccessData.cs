 namespace RelaSharp
 {   
    class AccessData<T> // Not sure if encapsulation provided by this type is desirable or right structure. 
    {
        public int LastStoredThreadId { get; private set; }
        public long LastStoredThreadClock { get; private set; }
        public VectorClock ReleasesToAcquire { get; private set; }
        public VectorClock LastSeen { get; private set; }
        
        public T Payload { get; private set; }
        public bool IsInitialized { get; private set; }

        public AccessData(int numThreads)
        {
            ReleasesToAcquire = new VectorClock(numThreads);
            LastSeen = new VectorClock(numThreads);
        }

        public void RecordStore(int threadIdx, long threadClock, T payload)
        {
            LastSeen.SetAllClocks(VectorClock.BeforeAllTimes);
            LastSeen[threadIdx] = threadClock;
            LastStoredThreadId = threadIdx;
            LastStoredThreadClock = threadClock;
            IsInitialized = true;
            Payload = payload;
        }

        public void RecordLoad(int threadIdx, long threadClock)
        {
            LastSeen[threadIdx] = threadClock;
        }
    }
 }
