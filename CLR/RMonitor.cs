using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RelaSharp.CLR
{
    static class RMonitor
    {
        private static Dictionary<Object, MonitorInstance> _lockToMonitor = new Dictionary<Object, MonitorInstance>();

        private static MonitorInstance GetMonitorInstance(Object lockObject)
        {
            MonitorInstance instance;
            if(!_lockToMonitor.TryGetValue(lockObject, out instance))
            {
                instance = new MonitorInstance();
                _lockToMonitor.Add(lockObject, instance);
            }
            return instance;
        }

        public static void Enter(Object lockObject, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Enter(lockObject, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Enter(Object lockObject, ref bool lockTaken)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Enter(lockObject, ref lockTaken);
        }

        public static void Exit(Object lockObject, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Exit(lockObject, memberName, sourceFilePath, sourceLineNumber);
        }

        public static bool IsEntered(Object lockObject)
        {
            throw new NotImplementedException();
        }

        public static void Pulse(Object lockObject, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Pulse(memberName, sourceFilePath, sourceLineNumber);
        }

        public static void PulseAll(Object lockObject, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.PulseAll(memberName, sourceFilePath, sourceLineNumber);
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

        public static void Wait(Object lockObject, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var instance = GetMonitorInstance(lockObject);
            instance.Wait(lockObject, memberName, sourceFilePath, sourceLineNumber);
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