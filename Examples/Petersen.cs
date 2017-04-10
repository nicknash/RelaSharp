using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class Petersen : IRelaExample 
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        public string Name => "Petersen Mutex";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;
        private MemoryOrdered<int> flag0;
        private MemoryOrdered<int> flag1;
        private MemoryOrdered<int> victim;
        private MemoryOrder MemoryOrder => ActiveConfig.MemoryOrder;
        public IReadOnlyList<Action> ThreadEntries { get; private set;}
        int _threadsPassed;

        public Petersen()
        {
            ThreadEntries = new List<Action>{Thread0,Thread1};
            var configList = new List<SimpleConfig>{new SimpleConfig("All operations acquire-release", MemoryOrder.Relaxed, true), 
                                                    new SimpleConfig("All operations sequentially consistent", MemoryOrder.SequentiallyConsistent, false)};
            _configs = configList.GetEnumerator();
        }

        private void Thread0()
        {
            flag0.Store(1, MemoryOrder);
            victim.Store(0, MemoryOrder);            
            while(flag1.Load(MemoryOrder) == 1 && victim.Load(MemoryOrder) == 0) ;        
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");            
            flag0.Store(0, MemoryOrder);
            --_threadsPassed;
        }

        private void Thread1()
        {
            flag1.Store(1, MemoryOrder);
            victim.Store(1, MemoryOrder);            
            while(flag0.Load(MemoryOrder) == 1 && victim.Load(MemoryOrder) == 1) ;        
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");
            flag1.Store(0, MemoryOrder);
            --_threadsPassed;
        }

        public void OnFinished()
        {

        }
        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        private void PrepareForNewConfig()
        {
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
            victim = new MemoryOrdered<int>();
            _threadsPassed = 0;
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }

}