using System.Runtime.CompilerServices;

namespace RelaSharp
{
    static class CLRAtomicFactory
    {
        public static Atomic<T> GetAtomic<T>(ref CLRAtomic<T> data)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            return data.__internal;
        }

        public static Atomic32 GetAtomic32(ref CLRAtomic32 data)
        {
            if(data == null)
            {
                data = new CLRAtomic32();
            }
            return data.__internal;
        }
  
        public static Atomic64 GetAtomic64(ref CLRAtomic64 data)
        {
            if(data == null)
            {
                data = new CLRAtomic64();
            }
            return data.__internal;
        }
    }

    static class RVolatile
    {
        public static T Read<T>(ref CLRAtomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            return atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref CLRAtomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            atomic.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}