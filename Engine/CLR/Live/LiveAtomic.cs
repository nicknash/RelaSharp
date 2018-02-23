using System.Runtime.CompilerServices;
using System.Threading;

namespace RelaSharp.CLR.Live
{
    class LiveAtomic<T> : IAtomic<T> where T : class
    {
        private T _data;

        public bool CompareExchange(T newData, T comparand, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.CompareExchange(ref _data, newData, comparand) == newData;

        public T Exchange(T newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.Exchange(ref _data, newData);       

        public T Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            switch(mo)
            {
                case MemoryOrder.Acquire:
                    return Volatile.Read(ref _data);
                case MemoryOrder.Relaxed:
                    return _data;
                default:
                    throw new EngineException($"LiveAtomicInt.Load should never be called with memory order {mo}.");
            }
        }

        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            switch (mo)
            {
                case MemoryOrder.Release:
                    Volatile.Write(ref _data, data);
                    break;
                case MemoryOrder.Relaxed:
                    _data = data;
                    break;
                default:
                    throw new EngineException($"LiveAtomicInt.Store should never be called with memory order {mo}.");
            }
        }
    }
}