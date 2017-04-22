using System;
using System.Collections.Generic;
using RelaSharp.Threading;

namespace RelaSharp.Examples
{
    public class LostWakeUp : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Store Load Re-ordering example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;

        private object lockObject;

        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        public LostWakeUp()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("Lost Wake Up", MemoryOrder.Relaxed, true)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
            RMonitor.Pulse(lockObject);
        }

        public void Thread2()
        {
            RMonitor.Wait(lockObject);
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            lockObject = new Object();
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
}
