using System;
using System.Threading;

namespace RelaSharp.CLR.Live
{
    class RealMonitor : IMonitor
    {
        public static readonly RealMonitor TheInstance = new RealMonitor();
        public void Enter(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Monitor.Enter(lockObject);
        }

        public void Enter(object lockObject, ref bool lockTaken)
        {
            Monitor.Enter(lockObject, ref lockTaken);
        }

        public void Exit(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Monitor.Exit(lockObject);
        }

        public void Pulse(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Monitor.Pulse(lockObject);
        }

        public void PulseAll(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            Monitor.PulseAll(lockObject);
        }

        public bool Wait(object lockObject, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            return Monitor.Wait(lockObject);
        }
    }
}