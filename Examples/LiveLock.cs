using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class LiveLock : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Livelock example (fragment of Petersen lock)";
        public string Description => "All operations sequentially consistent";
        public bool ExpectedToFail => true;
        private static TestEnvironment TE = TestEnvironment.TE;
        private MemoryOrdered<int> interested0;
        private MemoryOrdered<int> interested1;
        private bool _hasRun;

        public LiveLock()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
            interested0.Store(1, MemoryOrder.SequentiallyConsistent);
            while(interested1.Load(MemoryOrder.SequentiallyConsistent) == 1) ;
            interested0.Store(0, MemoryOrder.SequentiallyConsistent);
        }

        public void Thread2()
        {
            interested1.Store(1, MemoryOrder.SequentiallyConsistent);
            while(interested0.Load(MemoryOrder.SequentiallyConsistent) == 1) ;
            interested1.Store(0, MemoryOrder.SequentiallyConsistent);        
        }

        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            interested0 = new MemoryOrdered<int>();
            interested1 = new MemoryOrdered<int>();
        }

        public bool SetNextConfiguration()
        {
            bool oldHasRun = _hasRun;
            PrepareForNewConfig();
            _hasRun = true;
            return !oldHasRun;
        }
    }
}
