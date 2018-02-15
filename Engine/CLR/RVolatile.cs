using System.Runtime.CompilerServices;

namespace RelaSharp.CLR
{
    public static class RVolatile
    {
        public static T Read<T>(ref CLRAtomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            return atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref CLRAtomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }
       public static int Read(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            return atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomic32 data, int newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static long Read(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            return atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomic64 data, long newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}