using System;
using System.Collections.Generic;
using RelaSharp.Threading;

namespace RelaSharp.Examples
{
    public class Deadlock : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Deadlock detection example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;

        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        private Object lockObjectA = new object();
        private Object lockObjectB = new object();

        public Deadlock()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("Simple deadlock", MemoryOrder.Relaxed, true)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
            RMonitor.Enter(lockObjectA);
            RMonitor.Enter(lockObjectB);
            RMonitor.Exit(lockObjectB);
            RMonitor.Exit(lockObjectA);
        }

        public void Thread2()
        {
            RMonitor.Enter(lockObjectB);
            RMonitor.Enter(lockObjectA);
            RMonitor.Exit(lockObjectA);
            RMonitor.Exit(lockObjectB);
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
    
}