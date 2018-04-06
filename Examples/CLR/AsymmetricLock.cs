using System;
using System.Collections.Generic;
using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    public class AsymmetricLock : IRelaExample 
    {
        class AsymmetricLockInternal // TODO, translate
        {
            class LockCookie
            {
                internal LockCookie(int threadId)
                {
                    ThreadId = threadId;
                    Taken = false;
                }

                public void Exit()
                {
                    Taken = false;
                }

                internal readonly int ThreadId;
                internal bool Taken; // TODO annotate!
            }

            LockCookie _current = new LockCookie(-1);

            LockCookie Enter()
            {
                int currentThreadId = Environment.CurrentManagedThreadId;

                LockCookie entry = _current;

                if (entry.ThreadId == currentThreadId)
                {
                    entry.Taken = true;
                
                    if (RVolatile.Read(ref _current) == entry)
                    {
                        return entry;
                    }
                    entry.Taken = false;
                }
                return EnterSlow();
            }    
  
            private object _lockObj = new object();

            private LockCookie EnterSlow()
            {
                RMonitor.Enter(_lockObj);
                var oldEntry = _current;
                _current = new LockCookie(Environment.CurrentManagedThreadId);
                RInterlocked.MemoryBarrierProcessWide();
                while (oldEntry.Taken)
                {
                    RE.Yield();
                }
                _current.Taken = true;
                RMonitor.Exit(_lockObj);
                return _current;
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Quickly re-acquirable lock via interprocessor interrupt";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => true;
        private static IRelaEngine RE = RelaEngine.RE;
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
        }

        public void SlowThread()
        {
                 
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
