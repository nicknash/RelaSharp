using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class AsymmetricLock : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Asymmetric lock via Interprocessor Interrupt";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => true;
        private static IRelaEngine RE = RelaEngine.RE;
        private Atomic<int> interestedF;
        private Atomic<int> interestedS;
        private Atomic<int> victim;
        private int _threadsPassed;
        private bool _hasRun;

        public AsymmetricLock()
        {
            ThreadEntries = new List<Action> {FastThread,SlowThread};
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void FastThread()
        {
            interestedF.Store(1, MemoryOrder.Relaxed);
            victim.Store(0, MemoryOrder.Relaxed);
            while(interestedS.Load(MemoryOrder.Relaxed) == 1 && victim.Load(MemoryOrder.Relaxed) == 0) 
            {
                RE.Yield();
            }
            RE.Assert(_threadsPassed == 0, $"Fast thread entered while slow thread in critical section! ({_threadsPassed})");
            _threadsPassed++;
            interestedF.Store(0, MemoryOrder.Relaxed);
            _threadsPassed--;
        }

        public void SlowThread()
        {
            interestedS.Store(1, MemoryOrder.Relaxed);
            victim.Store(1, MemoryOrder.Relaxed);
            while(interestedF.Load(MemoryOrder.Relaxed) == 1 && victim.Load(MemoryOrder.Relaxed) == 1) 
            {
                RE.Yield();
            }
            RE.Assert(_threadsPassed == 0, $"Slow thread entered while fast thread in critical section! ({_threadsPassed})");
            _threadsPassed++;
            interestedS.Store(0, MemoryOrder.Relaxed);
            _threadsPassed--;
                 
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            interestedF = new Atomic<int>();
            interestedS = new Atomic<int>();
            victim = new Atomic<int>();
            _threadsPassed = 0;
        }

        public bool SetNextConfiguration()
        {
            bool oldHasRun = _hasRun;
            PrepareForNewConfig();
            _hasRun = true;
            return !oldHasRun;
        }
    }
}
