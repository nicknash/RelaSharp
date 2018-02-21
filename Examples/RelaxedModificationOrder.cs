using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class RelaxedModificationOrder : IRelaExample
    {
        private static RelaEngine RE = RelaEngine.RE;
        public string Name => "Single modification order example, even with all operations relaxed";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        private Atomic<int> x;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}
    
        public RelaxedModificationOrder()
        {
            ThreadEntries = new List<Action> {Thread0, Thread1};
            var configList = new List<SimpleConfig>{new SimpleConfig("Relaxed operations", MemoryOrder.AcquireRelease, false)}; 
            _configs = configList.GetEnumerator();
        }

        public void Thread0()
        {
            x.Store(1, MemoryOrder.Relaxed);
            x.Store(2, MemoryOrder.Relaxed);  
            x.Store(3, MemoryOrder.Relaxed);  
            x.Store(4, MemoryOrder.Relaxed);  
            x.Store(5, MemoryOrder.Relaxed);  
        }

        public void Thread1()
        {
            if(x.Load(MemoryOrder.Relaxed) == 3)
            {
                RE.Assert(x.Load(MemoryOrder.Relaxed) >= 3, "x should be at least 3");
            }
        }

        public void Thread2()
        {
            
        }
    
        public void OnBegin()
        {
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
            x = new Atomic<int>();
        }
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
    
}