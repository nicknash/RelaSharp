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
            // All writes are release in the CLR, but specify them as Relaxed anyway.
            // This is a compromise that feels OK, it means that LiveAtomics won't use Volatile unnecessarily,
            // and it's probably very bad practise to rely upon stores being release in the CLR implicitly in the first place.            
            var atomic = CLRAtomic<T>.Get(ref data);
            atomic.Store(newValue, MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static int Read(ref CLRAtomicInt data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicInt.Get(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomicInt data, int newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicInt.Get(ref data);
            // All writes are release in the CLR, but see the comment above.
            atomic.Store(newValue, MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }       

        public static long Read(ref CLRAtomicLong data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicLong.Get(ref data);
            return atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(ref CLRAtomicLong data, long newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicLong.Get(ref data);
            // All writes are release in the CLR, but see the comment above.
            atomic.Store(newValue, MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }       
    }
}