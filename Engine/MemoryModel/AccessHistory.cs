using System;

namespace RelaSharp.MemoryModel
{  
    class AccessHistory<T>
    {
        private readonly AccessDataPool<T> _history;
        private readonly int _numThreads;
        private readonly ILookback _lookback;

        public T CurrentValue => _history[_history.CurrentIndex].Payload;

        public AccessHistory(int length, int numThreads, ILookback lookback)
        {
            _history = new AccessDataPool<T>(length, numThreads);
            _numThreads = numThreads;
            _lookback = lookback;
        }
        
        public void RecordRMWStore(T data, MemoryOrder mo, ShadowThread runningThread)
        {
            RecordStore(data, mo, runningThread, true);
        }

        public void RecordStore(T data, MemoryOrder mo, ShadowThread runningThread)
        {
            var previous = _history[_history.CurrentIndex - 1];
            RecordStore(data, mo, runningThread, previous.IsInitialized && previous.LastStoredThreadId == runningThread.Id);
        }

        private void RecordStore(T data, MemoryOrder mo, ShadowThread runningThread, bool isReleaseSequence)
        {
            var storeTarget = _history.GetNext();
            storeTarget.RecordStore(runningThread.Id, runningThread.Clock, mo, data);

            bool isAtLeastRelease = mo == MemoryOrder.Release || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            
            // Here 'sourceClock' is the clock that other threads must synchronize with if they read-acquire this data.
            // If this store is a release (or stronger), then those threads must synchronize with the latest clocks that 
            // this thread has synchronized with (i.e. the releases it has acquired: runningThread.ReleasesAcquired).
            // Otherwise, if this store is relaxed, then those threads need only synchronize with the latest release fence of this thread
            // (i.e. runningThread.FenceReleasesAcquired)
            var sourceClock = isAtLeastRelease ? runningThread.ReleasesAcquired : runningThread.FenceReleasesAcquired;
            
            var previous = _history[_history.CurrentIndex - 1];
            var targetClock = storeTarget.ReleasesToAcquire;
            if(isReleaseSequence)
            {
                targetClock.Assign(previous.ReleasesToAcquire);
                targetClock.Join(sourceClock);
            }
            else
            {
                targetClock.Assign(sourceClock);
            }
        }

        public T RecordRMWLoad(MemoryOrder mo, ShadowThread runningThread)
        {
            var loadData = _history[_history.CurrentIndex];
            return RecordLoad(mo, runningThread, loadData);
        }

        public T RecordPossibleLoad(MemoryOrder mo, ShadowThread runningThread)
        {
            var loadData = GetPossibleLoad(runningThread.ReleasesAcquired, runningThread.Id, mo);
            return RecordLoad(mo, runningThread, loadData);
        }

        private T RecordLoad(MemoryOrder mo, ShadowThread runningThread, AccessData<T> loadData)
        {
            loadData.RecordLoad(runningThread.Id, runningThread.Clock);
            bool isAtLeastAcquire = mo == MemoryOrder.Acquire || mo == MemoryOrder.AcquireRelease || mo == MemoryOrder.SequentiallyConsistent;
            
            // Here 'destinationClock' is the clock that must synchronize with the last release to this data. 
            // If this load is an acquire (or stronger), then this thread's clock must synchronize with the last release
            // (i.e. it should acquire the release to this data so runningThread.ReleasesAcquired must update).
            // Otherwise, if this load is relaxed, then other threads must only synchronize with the last release to this data 
            // if an acquire fence is issued. So to allow for updating the releases acquired by this thread in the case of an acquire 
            // fence being issued at some point, update runningThread.FenceReleasesToAcquire 
            var destinationClock = isAtLeastAcquire ? runningThread.ReleasesAcquired : runningThread.FenceReleasesToAcquire;
            destinationClock.Join(loadData.ReleasesToAcquire);  
            return loadData.Payload;
        }

        private AccessData<T> GetPossibleLoad(VectorClock releasesAcquired, int threadId, MemoryOrder mo)
        {
            int maxLookback = 0;
            for(maxLookback = 0; maxLookback < _history.SizeOccupied; ++maxLookback)
            {
                int j = _history.CurrentIndex - maxLookback;
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
            }
            int lookback = _lookback.ChooseLookback(maxLookback);
            return _history[_history.CurrentIndex - lookback];
        }
    }
}
