namespace RelaSharp
{    
    class InternalMemoryOrdered<T> 
    {
        private AccessHistory<T> _history;

        public InternalMemoryOrdered(int historyLength, int numThreads)
        {
            _history = new AccessHistory<T>(historyLength, numThreads);
        }

        public void Store(T data, MemoryOrder mo)
        {
            _history.RecordStore(mo, data);
        }

        public T Load(MemoryOrder mo)
        {
            T result = _history.RecordPossibleLoad(mo);
            return result;
        }
        // Atomics ...
        // TODO: Need lock(..), fence, cmp exch, wrappers...    
    }
}

