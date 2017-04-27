using System.Runtime.CompilerServices;

namespace RelaSharp
{
    static class RUnordered
    {
        public static T Read<T>(ref CLRAtomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref CLRAtomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static int Read(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomic32 data, int newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static long Read(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomic64 data, long newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            // All writes are release in the CLR
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       
    }
}