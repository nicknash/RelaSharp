using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class StoreLoad : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Store Load Re-ordering example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static IRelaEngine RE = RelaEngine.RE;
        private Atomic<int> x0, x1;
        private int y0, y1;

        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        public StoreLoad()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("All operations relaxed", MemoryOrder.Relaxed, true), 
                                                    new SimpleConfig("All operations acquire-release", MemoryOrder.AcquireRelease, true),
                                                    new SimpleConfig("All operations sequentially consistent", MemoryOrder.SequentiallyConsistent, false)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
            x0.Store(1, ActiveConfig.MemoryOrder);
            y0 = x1.Load(ActiveConfig.MemoryOrder);        
        }

        public void Thread2()
        {
            x1.Store(1, ActiveConfig.MemoryOrder);
            y1 = x0.Load(ActiveConfig.MemoryOrder);
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
            RE.Assert(y0 != 0 || y1 != 0, "Both of y0 and y1 are zero! (store load reordering!)");
        }

        private void PrepareForNewConfig()
        {
            x0 = new Atomic<int>();
            x1 = new Atomic<int>();
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
}
