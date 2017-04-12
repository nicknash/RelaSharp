namespace RelaSharp
{    
    class InternalMemoryOrdered<T> 
    {
        private AccessHistory<T> _history;

        public T CurrentValue => _history.CurrentValue;

        public InternalMemoryOrdered(int historyLength, int numThreads)
        {
            _history = new AccessHistory<T>(historyLength, numThreads);
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
            if(loadedData.Equals(comparand))
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

