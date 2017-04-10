using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class TotalOrder : IRelaExample
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        public string Name => "Total order test";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        private MemoryOrdered<int> a, b;
        private int c, d;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}
    
        public TotalOrder()
        {
            ThreadEntries = new List<Action> {Thread0, Thread1, Thread2, Thread3};
            var configList = new List<SimpleConfig>{new SimpleConfig("All operations acquire-release", MemoryOrder.Relaxed, true), 
                                                    new SimpleConfig("All operations sequentially consistent", MemoryOrder.SequentiallyConsistent, false)};
            _configs = configList.GetEnumerator();
        }

        public void Thread0()
        {
            a.Store(1, ActiveConfig.MemoryOrder);
        }

        public void Thread1()
        {
            b.Store(1, ActiveConfig.MemoryOrder);
        } 

        public void Thread2()
        {
            if(a.Load(ActiveConfig.MemoryOrder) == 1 && b.Load(ActiveConfig.MemoryOrder) == 0)
            {
                c = 1;
            }
        }

        public void Thread3()
        {
            if(b.Load(ActiveConfig.MemoryOrder) == 1 && a.Load(ActiveConfig.MemoryOrder) == 0)
            {
                d = 1;
            }
        }

        public void OnFinished()
        {
            TE.Assert(c + d != 2, $"c + d == {c + d} ; neither of Thread0 or Thread1 ran first!");            
        }
        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        private void PrepareForNewConfig()
        {
            a = new MemoryOrdered<int>();
            b = new MemoryOrdered<int>();
            c = d = 0;
        }
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
    
}