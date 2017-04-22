using System;
using System.Collections.Generic;
using RelaSharp.Threading;

namespace RelaSharp.Examples
{
    public class LostWakeUp : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Lost Wake Up example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;

        private object _lockObject;

        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;

        private class Config
        {
            public readonly string Description;
            public readonly int NumPulses;
            public readonly int NumWaitingThreads;
            public readonly bool ExpectedToFail;

            public Config(string description, int numPulses, int numWaitingThreads, bool expectedToFail)
            {
                Description = description;
                NumPulses = numPulses;
                NumWaitingThreads = numWaitingThreads;
                ExpectedToFail = expectedToFail;
            }
        }

        public LostWakeUp()
        {
            var configList = new List<Config>{new Config("Lost Wake Up: 1 Pulse, 1 Waiting Thread", 1, 1, true)
                                             ,new Config("Lost Wake Up: 4 Pulses, 1 Waiting Thread", 4, 1, true)
                                             ,new Config("Lost Wake Up: 4 Pulses, 4 Waiting Thread", 4, 4, true)
                                             };
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
            RMonitor.Enter(_lockObject);
            RMonitor.Pulse(_lockObject);
            RMonitor.Exit(_lockObject);

            RMonitor.Enter(_lockObject);
            RMonitor.Pulse(_lockObject);
            RMonitor.Exit(_lockObject);

            RMonitor.Enter(_lockObject);
            RMonitor.Pulse(_lockObject);
            RMonitor.Exit(_lockObject);

            RMonitor.Enter(_lockObject);
            RMonitor.Pulse(_lockObject);
            RMonitor.Exit(_lockObject);
        }

        public void Thread2()
        {
            RMonitor.Enter(_lockObject);            
            RMonitor.Wait(_lockObject);
            RMonitor.Exit(_lockObject);    
        }

 
        private Action MakePulsingThread(int numPulses)
        {
            return () => PulsingThread(numPulses);
        }

        private Action MakeWaitingThread()
        {
            return () => WaitingThread();
        }

        private void PulsingThread(int numPulses)
        {
            for (int i = 0; i < numPulses; ++i)
            {
                RMonitor.Enter(_lockObject);
                RMonitor.Pulse(_lockObject);
                RMonitor.Exit(_lockObject);
            }
        }

        public void WaitingThread()
        {
            RMonitor.Enter(_lockObject);            
            RMonitor.Wait(_lockObject);
            RMonitor.Exit(_lockObject);    
        }


        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

         private void SetupActiveConfig()
        {
            var threadEntries = new List<Action>() { MakePulsingThread(ActiveConfig.NumPulses)};
            for(int i = 0; i < ActiveConfig.NumWaitingThreads; ++i)
            {
                threadEntries.Add(MakeWaitingThread());
            }
            ThreadEntries = threadEntries;                
        }

        private void PrepareForNewConfig()
        {
            _lockObject = new object();
        }
        
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            if(ActiveConfig != null)
            {
                SetupActiveConfig();
            }
            return moreConfigurations;
        }
   }
}
