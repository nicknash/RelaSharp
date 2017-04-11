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

        public bool CompareExchange(T comparand, T newData, MemoryOrder mo, ShadowThread runningThread)
        {
            T data = _history.RecordPossibleLoad(mo, runningThread);
            if(data.Equals(comparand))
            {
                _history.RecordStore(newData, mo, runningThread);
                return true;
            }
            return false;
        }
    }
}

