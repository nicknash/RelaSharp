namespace RelaSharp
{    
    class InternalMemoryOrdered<T> 
    {
        private AccessHistory<T> _history;

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
        // Atomics ...
        // TODO: Need lock(..), fence, cmp exch, wrappers...    
    }
}

