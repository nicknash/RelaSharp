using System;
using System.Collections.Generic;

namespace RelaSharp.Threading
{
    class MonitorInstance
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private VectorClock _lockClock;

        private Queue<ShadowThread> _waiting;
        private Queue<ShadowThread> _ready;

        private ShadowThread _heldBy;

        public MonitorInstance()
        {
            _waiting = new Queue<ShadowThread>();
            _ready = new Queue<ShadowThread>();
        }

        public void Enter()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(_heldBy == null)
            {
                _heldBy = runningThread;
                runningThread.ReleasesAcquired.Join(_lockClock);
                // Event log
            }
            else
            {
                _ready.Enqueue(runningThread);
                // Event log
                // Tell scheduler thread is blocked.
                TE.MaybeSwitch(ThreadState.Waiting);
            }
        }

        public void Enter(ref bool lockTaken)
        {
            if(lockTaken)
            {
                throw new ArgumentException($"{nameof(lockTaken)} must be passed as false");
            }
        }

        public void Exit()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(_heldBy != runningThread)
            {
                // FAIL TEST.
            }
            // Event log
            _lockClock.Join(_heldBy.ReleasesAcquired);
            _heldBy = null;
            TE.MaybeSwitch();
        }

        public void Pulse()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(_heldBy != runningThread)
            {
                // FAIL TEST.
            }
            // event log
            if(_waiting.Count > 0)
            {
                _ready.Enqueue(_waiting.Dequeue());
            }

        }

        public void PulseAll()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(_heldBy != runningThread)
            {
                // FAIL TEST.
            }
            foreach(var thread in _waiting)
            {
                _ready.Enqueue(thread);
            }
            // event log
        }

        public bool Wait()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(_heldBy != runningThread)
            {
                // FAIL TEST.
            }
            // Event log
            _waiting.Enqueue(runningThread);
            Exit();
            // Tell scheduler thread is blocked
            Enter();
            // Event log
            return true;
        }
    }
}