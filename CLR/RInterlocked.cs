using System.Runtime.CompilerServices;

namespace RelaSharp.CLR
{      
    class RInterlocked // TODO: Check Joe Duffy -- do these imply a SeqCstFence ?
    {
        public static bool CompareExchange<T>(ref CLRAtomic<T> data, T newData, T comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange<T>(ref CLRAtomic<T> data, T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic<T>.Get(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
        public static int Increment(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            return atomic.Increment(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }

        public static int Decrement(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            return atomic.Decrement(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }

        public static void Exchange(ref CLRAtomic32 data, int newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
 
        public static bool CompareExchange(ref CLRAtomic32 data, int newData, int comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic32.Get(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
 
        public static long Increment(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            return atomic.Increment(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }

        public static long Decrement(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            return atomic.Decrement(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
        public static long Read(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            return atomic.Load(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
        public static void Exchange(ref CLRAtomic64 data, long newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
        public static bool CompareExchange(ref CLRAtomic64 data, long newData, long comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomic64.Get(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber); 
        }
        
        public static void MemoryBarrier([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Fence.Insert(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}