using System;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    static class RVolatile<T> where T : IEquatable<T>
    {
        public static T Read(MemoryOrdered<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write(MemoryOrdered<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            data.Store(newValue, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}