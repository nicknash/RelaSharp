using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class SimpleAcquireRelease : IRelaExample
    {
        private class Config
        {
            public readonly string Description;
            public readonly MemoryOrder StoreMemoryOrder;
            public readonly MemoryOrder LoadMemoryOrder;
            public readonly bool ExpectedToFail;

            public Config(string description, MemoryOrder storeMemoryOrder, MemoryOrder loadMemoryOrder, bool expectedToFail)
            {
                Description = description;
                StoreMemoryOrder = storeMemoryOrder;
                LoadMemoryOrder = loadMemoryOrder;
                ExpectedToFail = expectedToFail;
            }
        }
        public string Name => "Simple demonstration of acquire and release semantics.";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        public IReadOnlyList<Action> ThreadEntries { get; private set;}
        private static RelaEngine TR = RelaEngine.RE;        
        private Atomic<int> _flag;
        private Atomic <int> _x;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;

        public SimpleAcquireRelease()
        {
            ThreadEntries = new List<Action> { ReleaseThread, AcquireThread};
            var configList = new List<Config>{new Config("Final store relaxed, result-load relaxed", MemoryOrder.Relaxed, MemoryOrder.Relaxed, true), 
                                              new Config("Final store release, result-load relaxed.", MemoryOrder.Release, MemoryOrder.Relaxed, true),
                                              new Config("Final store release, result-load acquire.", MemoryOrder.Release, MemoryOrder.Acquire, false),
                                              new Config("Final store sequentially consistent, result-load sequentially consistent.", MemoryOrder.SequentiallyConsistent, MemoryOrder.SequentiallyConsistent, false)
                                             };
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        private void ReleaseThread()
        {
            _x.Store(23, MemoryOrder.Relaxed);
            _x.Store(22, MemoryOrder.Relaxed);
            _x.Store(21, MemoryOrder.Relaxed);
            _x.Store(2, MemoryOrder.Relaxed);
            _flag.Store(0, MemoryOrder.Relaxed);
            _flag.Store(1, ActiveConfig.StoreMemoryOrder);
        }

        private void AcquireThread()
        {
            while(_flag.Load(ActiveConfig.LoadMemoryOrder) == 0) 
            {
                TR.Yield();
            }
            int result = _x.Load(MemoryOrder.Relaxed);
            TR.Assert(result == 2, $"Expected to load 2 into result, but loaded {result} instead!");
        }

        public void OnBegin()
        {
            
        }
        public void OnFinished()
        {
        }
        private void PrepareForNewConfig()
        {
            _flag = new Atomic<int>();
            _x = new Atomic<int>();
        }
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }

}