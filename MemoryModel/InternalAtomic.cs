namespace RelaSharp
{    
    class InternalAtomic<T> 
    {
        private AccessHistory<T> _history;

        public T CurrentValue => _history.CurrentValue;

        public InternalAtomic(int historyLength, int numThreads, ILookback lookback)
        {
            _history = new AccessHistory<T>(historyLength, numThreads, lookback);
        }

        public void Store(T data, MemoryOrder mo, ShadowThread runningThread)
        {
            _history.RecordStore(data, mo, runningThread);
        }

        public T Load(MemoryOrder mo, ShadowThread runningThread)
        {
            T result = _history.RecordPossibleLoad(mo, runningThread);
            return result;
        }

        public bool CompareExchange(T newData, T comparand, MemoryOrder mo, ShadowThread runningThread, out T loadedData)
        {
            loadedData = _history.RecordRMWLoad(mo, runningThread);
            if(loadedData == null && comparand == null || loadedData != null && loadedData.Equals(comparand))
            {
                _history.RecordRMWStore(newData, mo, runningThread);
                return true;
            }
            return false;
        }

        public T Exchange(T newData, MemoryOrder mo, ShadowThread runningThread)
        {
            var oldData = _history.RecordRMWLoad(mo, runningThread);
            _history.RecordRMWStore(newData, mo, runningThread);
            return oldData;
        }
    }
}

