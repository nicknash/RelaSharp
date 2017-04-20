using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelaSharp.Threading
{
    class MonitorInstance
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private VectorClock _lockClock;

        private Queue<int> _waiting;
        private Queue<int> _ready;

        private ShadowThread _heldBy;
        private int _timesEntered;

        public MonitorInstance()
        {
            _waiting = new Queue<int>();
            _ready = new Queue<int>();

        }

        private bool IsHeld => _timesEntered > 0;

        public void Enter([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            if(IsHeld) 
            {
                if(_heldBy != runningThread)
                {
                    _timesEntered++;
                }
                else
                {
                    while(IsHeld)
                    {
                        TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Enter (waiting)."); // TODO give lock-obj addr
                        TE.ThreadWaiting();
                    }
                }
            }
            AcquireLock(runningThread, memberName, sourceFilePath, sourceLineNumber);
        }

        private void AcquireLock(ShadowThread runningThread, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            runningThread.ReleasesAcquired.Join(_lockClock);
            _heldBy = runningThread;
            _timesEntered++;
            // TODO give lock-obj addr in event-log
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Enter: Lock-acquired ({_timesEntered}."); 
            // TE.MaybeSwitch() ?
        }

        private void ReleaseLock()
        {
            _lockClock.Join(_heldBy.ReleasesAcquired);
            _timesEntered--;
            if(_timesEntered == 0)
            {
                _heldBy = null;
            }
            // TE.MaybeSwitch() ?
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
                var lockHeldBy = _heldBy == null ? "Nobody" : $"{_heldBy.Id}";
                TE.FailTest($"Attempt to Monitor.Exit on thread {runningThread.Id}, but lock is held by {lockHeldBy}");
                return;
            }
            ReleaseLock();
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
            _waiting.Enqueue(runningThread.Id);
            // _waitingThreadIds.Add(runningThread.Id);
            Exit(); // maybe don't call this, but do same semantics..
            while(_ready.Count == 0 || _ready.Peek() != runningThread.Id/*  _waitingThreadIds.Contains(runningThread.Id)*/)
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