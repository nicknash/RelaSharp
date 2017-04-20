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

        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;

        private class Config
        {
            public readonly string Description;
            public readonly int NumThreads;
            public readonly bool ExpectedToFail;

            public object[] LockObjects;
            public Config(string description, int numThreads, bool expectedToFail)
            {
                Description = description;
                NumThreads = numThreads;
                ExpectedToFail = expectedToFail;
                LockObjects = new Object[numThreads];
                for(int i = 0; i < numThreads; ++i)
                {
                    LockObjects[i] = new Object();
                }
            }
        }

        public Deadlock()
        {
            var configList = new List<Config>{new Config("1 thread: deadlock impossible", 1, false),
                                              new Config("2 threads, with possible cyclic wait: deadlock expected", 2, true),
                                              new Config("4 threads, with possible cyclic wait: deadlock expected", 4, true),
                                              new Config("8 threads, with possible cyclic wait: deadlock expected", 8, true)
                                              };
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
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

        private Action MakeThread(int idx)
        {
            return () => LockingThread(idx);
        }

        private void LockingThread(int idx)
        {
            var myLock = ActiveConfig.LockObjects[idx];
            var nextLock = ActiveConfig.LockObjects[(idx + 1) % ActiveConfig.NumThreads];
            RMonitor.Enter(myLock);
            RMonitor.Enter(nextLock);
            RMonitor.Exit(nextLock);
            RMonitor.Exit(myLock);
        }

        private void SetupActiveConfig()
        {
            var threadEntries = new List<Action>();
            for(int i = 0; i < ActiveConfig.NumThreads; ++i)
            {
                threadEntries.Add(MakeThread(i));
            }
            ThreadEntries = threadEntries;                
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