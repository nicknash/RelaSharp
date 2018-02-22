using System.Runtime.CompilerServices;

namespace RelaSharp.CLR
{
    public static class RUnordered
    {
        public static T Read<T>(ref CLRAtomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) where T : class
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref CLRAtomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) where T : class
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static int Read(ref CLRAtomicInt data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicInt.Get(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomicInt data, int newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicInt.Get(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static long Read(ref CLRAtomicLong data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicLong.Get(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomicLong data, long newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicLong.Get(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       
    }
}