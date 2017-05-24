using System;
using System.Collections.Generic;

namespace RelaSharp.CLR
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
            _lockClock = new VectorClock(TE.NumThreads);
        }

        private bool IsHeld => _heldBy != null;

        public void Enter(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            var runningThread = Preamble();
            if (IsHeld && _heldBy != runningThread)
            {
                while (IsHeld)
                {
                    TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Enter: (waiting {_timesEntered})."); 
                    TE.ThreadWaiting(_heldBy.Id, lockObject);
                }
                TE.ThreadFinishedWaiting();
            }
            AcquireLock(runningThread, memberName, sourceFilePath, sourceLineNumber);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Enter: Lock-acquired ({_timesEntered})."); 
            TE.MaybeSwitch(); 
        }

        public void Enter(object lockObject, ref bool lockTaken)
        {
            if(lockTaken)
            {
                throw new ArgumentException($"{nameof(lockTaken)} must be passed as false");
            }
        }

        public void Exit(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            var runningThread = Preamble();
            if(_heldBy != runningThread)
            {
                FailLockNotHeld(memberName, sourceFilePath, sourceLineNumber, runningThread.Id, "Exit");
                return;
            }
            ReleaseLock(lockObject, memberName, sourceFilePath, sourceLineNumber);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Exit: Lock-released ({_timesEntered})."); 
            TE.MaybeSwitch();
        }

        public void Pulse(string memberName, string sourceFilePath, int sourceLineNumber)
        {
            var runningThread = Preamble();
            if(_heldBy != runningThread)
            {
                FailLockNotHeld(memberName, sourceFilePath, sourceLineNumber, runningThread.Id, "Pulse");
                return;
            }
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Pulse");
            if(_waiting.Count > 0)
            {
                _ready.Enqueue(_waiting.Dequeue());
            }
        }

        public void PulseAll(string memberName, string sourceFilePath, int sourceLineNumber)
        {            
            var runningThread = Preamble();
            if(_heldBy != runningThread)
            {
                FailLockNotHeld(memberName, sourceFilePath, sourceLineNumber, runningThread.Id, "PulseAll");
                return;
            }
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.PulseAll");
            while(_waiting.Count > 0)
            {
                _ready.Enqueue(_waiting.Dequeue());
            }
        }

        public bool Wait(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            var runningThread = Preamble();
            if(_heldBy != runningThread)
            {
                FailLockNotHeld(memberName, sourceFilePath, sourceLineNumber, runningThread.Id, "Pulse");
                return false;
            }
            _waiting.Enqueue(runningThread.Id);
            
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Wait (waiting).");
            ReleaseLock(lockObject, memberName, sourceFilePath, sourceLineNumber);
            while(_ready.Count == 0 || _ready.Peek() != runningThread.Id)
            {
                TE.ThreadWaiting(_heldBy.Id, lockObject);
            }
            _ready.Dequeue();
            TE.ThreadFinishedWaiting();
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Wait (woken).");
            while (IsHeld)
            {
                TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.Wait: (woken-waiting {_timesEntered}).");
                TE.ThreadWaiting(_heldBy.Id, lockObject);
            }
            TE.ThreadFinishedWaiting();
            AcquireLock(runningThread, memberName, sourceFilePath, sourceLineNumber);
            return true;
        }


        private void AcquireLock(ShadowThread runningThread, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            // TODO: inc clock?
            runningThread.ReleasesAcquired.Join(_lockClock);
            _heldBy = runningThread;
            _timesEntered++;
        }

        private void ReleaseLock(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            // TODO: inc clock?
            _lockClock.Join(_heldBy.ReleasesAcquired);
            _timesEntered--;
            if(_timesEntered == 0)
            {
                _heldBy = null;
            }
            TE.LockReleased(lockObject);
        }

        private ShadowThread Preamble()
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            return runningThread;
        }

        private void FailLockNotHeld(string memberName, string sourceFilePath, int sourceLineNumber, int runningThreadId, string operationAttempted)
        {
            var heldByDesc = _heldBy == null ? "Nobody" : $"{_heldBy.Id}";
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Monitor.{operationAttempted} (failure!).");
            TE.FailTest($"Attempt to Monitor.{operationAttempted} on thread {runningThreadId}, but lock is held by {heldByDesc}.");
        }
    }
}