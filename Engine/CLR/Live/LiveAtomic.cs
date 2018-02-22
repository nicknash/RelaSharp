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
            throw new EngineException("Load should never be called via a CLRAtomic");
        }

        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            throw new EngineException("Load should never be called via a CLRAtomic");
        }
    }
}