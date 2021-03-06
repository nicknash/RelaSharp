using System;
using System.Collections.Generic;
using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    public class AsymmetricLock : IRelaExample 
    {
        class AsymmetricLockInternal 
        {
            CLRAtomicInt _holdingThreadId;
            CLRAtomicInt _isHeld;
            public AsymmetricLockInternal()
            {
            }

            internal void Enter()
            {
                if (RUnordered.Read(ref _isHeld) == 1)
                {
                    int currentThreadId = Environment.CurrentManagedThreadId;
                    if (RUnordered.Read(ref _holdingThreadId) == currentThreadId)
                    {
                        return;
                    }
                }
                EnterSlow();
            }    
  
            private object _lockObj = new object();

            private void EnterSlow()
            {
                RMonitor.Enter(_lockObj); 
                RUnordered.Write(ref _holdingThreadId, Environment.CurrentManagedThreadId);
                RInterlocked.MemoryBarrierProcessWide();
                while (RUnordered.Read(ref _isHeld) == 1)
                {
                    RE.Yield();
                }
                RUnordered.Write(ref _isHeld, 1);
                RMonitor.Exit(_lockObj);
                return;
            }

            public void Exit()
            {
                RUnordered.Write(ref _isHeld, 0);
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Quickly re-acquirable lock via interprocessor interrupt";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => false; 
        private static IRelaEngine RE = RelaEngine.RE;
        private int _threadsPassed;
        private bool _hasRun;
        private AsymmetricLockInternal _asymLock;

        public AsymmetricLock()
        {
            ThreadEntries = new List<Action> {Thread0,Thread1};
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread0()
        {
            for (int i = 0; i < 2; ++i)
            {
                _asymLock.Enter(); 
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread0 entered while Thread1 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                _asymLock.Exit();
                _threadsPassed--;
            }
        }

        public void Thread1()
        {
            for (int i = 0; i < 2; ++i)
            {
                _asymLock.Enter();
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread1 entered while Thread0 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                _asymLock.Exit();
                _threadsPassed--;
            }
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            _threadsPassed = 0;
            _asymLock = new AsymmetricLockInternal();
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
