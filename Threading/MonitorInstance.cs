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
        private HashSet<int> _waitingThreadIds;

        private ShadowThread _heldBy;

        public MonitorInstance()
        {
            _waiting = new Queue<ShadowThread>();
            _waitingThreadIds = new HashSet<int>();
            _ready = new Queue<ShadowThread>(); // Seems to be no need for this?
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
                if(_heldBy != runningThread)
                {
                    while(_heldBy != null)
                    {
                        TE.ThreadWaiting();
                    }
                    _heldBy = runningThread;
                    runningThread.ReleasesAcquired.Join(_lockClock);
                }
                // TODO: should increment recursion count if heldBy == runningThread
                // TODO: Event log
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
                // _waitingThreadIds.Remove(_waiting.Dequeue().Id);
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
                // _waitingThreadIds.Remove(_waiting.Dequeue().Id);
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
            _waitingThreadIds.Add(runningThread.Id);
            Exit();
            while(_ready.Count == 0 || _ready.Peek().Id != runningThread.Id/*  _waitingThreadIds.Contains(runningThread.Id)*/)
            {
                TE.ThreadWaiting();
            }
            _ready.Dequeue();
            Enter();
            // Event log
            return true;
        }
    }
}