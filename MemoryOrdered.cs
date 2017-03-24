namespace RelaSharp
{
    class MemoryOrdered<T> // TODO: restrict to atomic types.
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private InternalMemoryOrdered<T> _memoryOrdered;

        private void MaybeInit()
        {
            if(_memoryOrdered == null)
            {
                _memoryOrdered = new InternalMemoryOrdered<T>(TE.HistoryLength, TE.NumThreads);
            }
        }

        public void Store(T data, MemoryOrder mo)
        {
            MaybeInit();
            TE.Scheduler();
            TE.RunningThread.IncrementClock();
            _memoryOrdered.Store(data, mo, TE.RunningThread);
        }

        public T Load(MemoryOrder mo)
        {
            MaybeInit();
            TE.Scheduler();
            TE.RunningThread.IncrementClock();
            return _memoryOrdered.Load(mo, TE.RunningThread);
        }
    }
}

