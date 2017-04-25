using System.Runtime.CompilerServices;

namespace RelaSharp
{
    static class RVolatile
    {
        public static T Read<T>(ref Atomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new Atomic<T>();
            }
            return data.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref Atomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new Atomic<T>();
            }
            data.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }

        public static long Read(ref Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new Atomic64();
            }
            return data.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

    }
}