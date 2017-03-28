using System;

namespace RelaSharp
{  
    class AccessHistory<T>
    {
        private readonly AccessDataPool<T> _history;
        private readonly int _numThreads;
        private readonly Random _random;

        public AccessHistory(int length, int numThreads)
        {
            _history = new AccessDataPool<T>(length, numThreads);
            _numThreads = numThreads;
            _random = new Random();
        }
        
        public void RecordStore(T data, MemoryOrder mo, ShadowThread runningThread)
        {
            var storeTarget = _history.GetNext();
            storeTarget.RecordStore(runningThread.Id, runningThread.Clock, mo, data);

            bool isAtLeastRelease = mo == MemoryOrder.Release || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            
            var sourceClock = isAtLeastRelease ? runningThread.VC : runningThread.Fenced;
            var previous = _history[_history.CurrentIndex - 1];
            bool isReleaseSequence = previous.IsInitialized && previous.LastStoredThreadId == runningThread.Id; // TODO: OR this is part of a read-modify-write
            if(isReleaseSequence)
            {
                storeTarget.ReleasesToAcquire.Assign(previous.ReleasesToAcquire);
                storeTarget.ReleasesToAcquire.Join(sourceClock);
            }
            else
            {
                storeTarget.ReleasesToAcquire.Assign(sourceClock);
            }
        }

        public T RecordPossibleLoad(MemoryOrder mo, ShadowThread runningThread)
        {
            var loadData = GetPossibleLoad(runningThread.VC, runningThread.Id, mo);
            loadData.RecordLoad(runningThread.Id, runningThread.Clock);
            bool isAtLeastAcquire = mo == MemoryOrder.Acquire || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            var destinationClock = isAtLeastAcquire ? runningThread.VC : runningThread.Fenced; // TODO: AcquireFenced
            destinationClock.Join(loadData.ReleasesToAcquire);  
            return loadData.Payload;
        }

        private AccessData<T> GetPossibleLoad(VectorClock releasesAcquired, int threadId, MemoryOrder mo)
        {
            int j = _history.CurrentIndex;
            int lookBack = _random.Next(_history.SizeOccupied);
            for(int i = 0; i < lookBack; ++i)
            {
                if(!_history[j].IsInitialized)
                {
                    throw new Exception("This should never happen: access to uninitialized variable.");
                }
                var accessData = _history[j];
                if(mo == MemoryOrder.SequentiallyConsistent && accessData.LastStoreWasSequentiallyConsistent)
                {
                    break;
                }
                // Has the loading thread synchronized-with this or a later release of the last storing thread to this variable?
                if(releasesAcquired[accessData.LastStoredThreadId] >= accessData.LastStoredThreadClock)
                {
                    // If so, this is the oldest load that can be returned, since this thread has synchronized-with 
                    // ("acquired a release" of) the storing thread at or after this store.
                    break;
                }
                // Has the loading thread synchronized-with any thread that has loaded this or a later value 
                // of this variable?
                if(releasesAcquired.AnyGreaterOrEqual(accessData.LastSeen))
                {
                    // If so, this is the oldest load that can be returned to the loading thread, otherwise it'd
                    // be going back in time, since it has synchronized-with ("acquired a release" of) a thread that has seen this or a later value 
                    // of this variable.
                    break;
                }
                --j;
            }
            return _history[j];
        }
    }
}
