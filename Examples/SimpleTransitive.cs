using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class SimpleTransitive : IRelaExample
    {
        private static RelaEngine RE = RelaEngine.RE;
        public string Name => "Acquire/Release transitive visibility example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        private Atomic<int> x, y, z;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}
    
        public SimpleTransitive()
        {
            ThreadEntries = new List<Action> {Thread0, Thread1, Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("Mixed operations", MemoryOrder.AcquireRelease, false)}; 
            _configs = configList.GetEnumerator();
        }

        public void Thread0()
        {
            x.Store(1, MemoryOrder.Release);
            x.Store(2, MemoryOrder.Release);  
        }

        public void Thread1()
        {
            if(x.Load(MemoryOrder.Acquire) == 2)
            {
                y.Store(1, MemoryOrder.Release);
            }
        }
        
        public void Thread2()
        {
            if(y.Load(MemoryOrder.Acquire) == 1)
            {
                RE.Assert(x.Load(MemoryOrder.Relaxed) == 2, "x should be 2");
            }
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
            y = new Atomic<int>();
            z = new Atomic<int>();
        }
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
    
}