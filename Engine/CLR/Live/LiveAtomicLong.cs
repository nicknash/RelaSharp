using System.Runtime.CompilerServices;
using System.Threading;

namespace RelaSharp.CLR.Live
{
    class LiveAtomicLong : IAtomicLong
    {
        private long _data;

        public long Add(long x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.Add(ref _data, x);

        public bool CompareExchange(long newData, long comparand, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.CompareExchange(ref _data, newData, comparand) == newData;
        

        public long Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.Decrement(ref _data);

        public long Exchange(long newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.Exchange(ref _data, newData);

        public long Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            => Interlocked.Increment(ref _data);

        public long Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            throw new EngineException("Load should never be called via a CLRAtomic");
        }

        public void Store(long data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            throw new EngineException("Store should never be called via a CLRAtomic");            
        }
    }
}