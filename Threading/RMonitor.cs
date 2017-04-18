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
            TE.Scheduler();
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
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            if(_heldBy != runningThread)
            {
                // FAIL TEST.
            }
            // Event log
            _lockClock.Join(_heldBy.ReleasesAcquired);
            _heldBy = null;
            TE.Scheduler();
        }

        public void Pulse()
        {
            TE.Scheduler();
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
            TE.Scheduler();
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
            TE.Scheduler();
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

    static class RMonitor
    {
        private static Dictionary<Object, MonitorInstance> _lockToMonitor = new Dictionary<Object, MonitorInstance>();

        private static MonitorInstance GetMonitorInstance(Object lockObject)
        {
            MonitorInstance instance;
            if(!_lockToMonitor.TryGetValue(lockObject, out instance))
            {
                instance = new MonitorInstance();
            }
            return instance;
        }

        public static void Enter(Object lockObject)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Enter();
        }

        public static void Enter(Object lockObject, ref bool lockTaken)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Enter(ref lockTaken);
        }

        public static void Exit(Object lockObject)
        {
            throw new NotImplementedException();
        }

        public static bool IsEntered(Object lockObject)
        {
            throw new NotImplementedException();
        }

        public static void Pulse(Object lockObject)
        {
            throw new NotImplementedException();

        }

        public static void PulseAll(Object lockObject)
        {
            throw new NotImplementedException();
        }

        public static bool TryEnter(Object lockObject)
        {
            throw new NotImplementedException();
        }
        public static void TryEnter(Object lockObject, ref bool lockTaken)
        {
            throw new NotImplementedException();
        }

        public static bool TryEnter(Object lockObject, int millisecondsTimeout)
        {
            throw new NotImplementedException();
        }

        public static void TryEnter(Object lockObject, int millisecondsTimeout, ref bool lockTaken)
        {
            throw new NotImplementedException();
        }
        public static bool TryEnter(Object lockObject, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public static void TryEnter(Object lockObject, TimeSpan timeout, ref bool lockTaken)
        {
            throw new NotImplementedException();
        }

        public static void Wait(Object lockObject)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Wait();
        }

        public static void Wait(Object lockObject, int millisecondsTimeout)
        {
            
        }

        public static void Wait(Object lockObject, int millisecondsTimeout, bool exitContext)
        {

        }

        public static void Wait(Object lockObject, TimeSpan timeout)
        {

        }

        public static void Wait(Object lockObject, TimeSpan timeout, bool exitContext)
        {

        }
    }
}