using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class Config
    {
        public readonly string Description;
        public readonly MemoryOrder MemoryOrder;
        public readonly bool UseExchange;
        public readonly bool ExpectedToFail;

        public Config(string description, MemoryOrder memoryOrder, bool useExchange, bool expectedToFail)
        {
            Description = description;
            MemoryOrder = memoryOrder;
            UseExchange = useExchange;
            ExpectedToFail = expectedToFail;
        }
    }
    class Petersen : IRelaExample 
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        public string Name => "Petersen Mutex";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private Atomic<int> flag0;
        private Atomic<int> flag1;
        private Atomic<int> victim;
        private MemoryOrder MemoryOrder => ActiveConfig.MemoryOrder;
        public IReadOnlyList<Action> ThreadEntries { get; private set;}
        int _threadsPassed;

        public Petersen()
        {
            ThreadEntries = new List<Action>{Thread0,Thread1};
            var configList = new List<Config>{new Config("All operations acquire-release", MemoryOrder.Relaxed, false, true), 
                                              new Config("All operations sequentially consistent", MemoryOrder.SequentiallyConsistent, false, false),
                                              new Config("Relaxed flag entry, Release flag exit, Acquire flag spin, acquire-release exchange on victim.", MemoryOrder.Relaxed, true, false)};
            _configs = configList.GetEnumerator();
        }

        private void Thread0()
        {
            flag0.Store(1, MemoryOrder);
            if(ActiveConfig.UseExchange)
            {
                victim.Exchange(0, MemoryOrder.AcquireRelease);
            }
            else
            {
                victim.Store(0, MemoryOrder);                            
            }
            while(flag1.Load(ActiveConfig.UseExchange ? MemoryOrder.Acquire : MemoryOrder) == 1 & victim.Load(MemoryOrder) == 0) TE.Yield();        
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");            
            flag0.Store(0, ActiveConfig.UseExchange ? MemoryOrder.Release : MemoryOrder);
            --_threadsPassed;
        }

        private void Thread1()
        {
            flag1.Store(1, MemoryOrder);
            if(ActiveConfig.UseExchange)
            {
                victim.Exchange(1, MemoryOrder.AcquireRelease);
            }
            else
            {
                victim.Store(1, MemoryOrder);                            
            }
            while(flag0.Load(ActiveConfig.UseExchange ? MemoryOrder.Acquire : MemoryOrder) == 1 && victim.Load(MemoryOrder) == 1) TE.Yield();        
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");
            flag1.Store(0, ActiveConfig.UseExchange ? MemoryOrder.Release : MemoryOrder);
            --_threadsPassed;
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
            flag0 = new Atomic<int>();
            flag1 = new Atomic<int>();
            victim = new Atomic<int>();
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