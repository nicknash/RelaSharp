using System;
using System.Collections.Generic;
using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    public class SafeAsymmetricLock : IRelaExample 
    {
        class SafeAsymmetricLockInternal 
        {
            internal class LockCookie
            {
                internal LockCookie(int threadId)
                {
                    ThreadId = threadId;
                    RUnordered.Write(ref Taken, 1);
                }

                public void Exit()
                {
                    RUnordered.Write(ref Taken, 0);
                }

                internal readonly int ThreadId;
                internal CLRAtomicInt Taken;
            }

            CLRAtomic<LockCookie> _current;

            public SafeAsymmetricLockInternal()
            {
            }

            internal LockCookie Enter()
            {
                int currentThreadId = Environment.CurrentManagedThreadId;

                LockCookie entry = RUnordered.Read(ref _current);

                if (entry?.ThreadId == currentThreadId)
                {
                    RUnordered.Write(ref entry.Taken, 1);
                
                    if (RVolatile.Read(ref _current) == entry)
                    {
                        return entry;
                    }
                    RUnordered.Write(ref entry.Taken, 0);
                }
                return EnterSlow();
            }    
  
            private object _lockObj = new object();

            private LockCookie EnterSlow()
            {
                RMonitor.Enter(_lockObj); 
                var oldEntry = RUnordered.Read(ref _current);
                RUnordered.Write(ref _current, new LockCookie(Environment.CurrentManagedThreadId));
                RInterlocked.MemoryBarrierProcessWide();
                while (oldEntry != null && RUnordered.Read(ref oldEntry.Taken) == 1)
                {
                    RE.Yield();
                }
                var current = RUnordered.Read(ref _current);
                RUnordered.Write(ref current.Taken, 1);
                RMonitor.Exit(_lockObj);
                return current;
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Quickly re-acquirable lock via interprocessor interrupt ('safe' version, only releasable by holding thread)";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => false; 
        private static IRelaEngine RE = RelaEngine.RE;
        private int _threadsPassed;
        private bool _hasRun;
        private SafeAsymmetricLockInternal _asymLock;

        public SafeAsymmetricLock()
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
                var c = _asymLock.Enter(); 
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread0 entered while Thread1 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                c.Exit();
                _threadsPassed--;
            }
        }

        public void Thread1()
        {
            for (int i = 0; i < 2; ++i)
            {
                var c = _asymLock.Enter();
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread1 entered while Thread0 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                c.Exit();
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
            _asymLock = new SafeAsymmetricLockInternal();
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
