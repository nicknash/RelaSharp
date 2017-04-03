using System;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class RVolatile<T> where T : IEquatable<T>
    {
        private MemoryOrdered<T> _data;
        public RVolatile()
        {
            _data = new MemoryOrdered<T>();
        }

        public T Read([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _data.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public void Write(T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _data.Store(newData, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}