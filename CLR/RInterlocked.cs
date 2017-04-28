using System.Runtime.CompilerServices;

namespace RelaSharp.CLR
{      
    class RInterlocked // TODO: Check Joe Duffy -- do these imply a SeqCstFence ?
    {
        public static bool CompareExchange<T>(ref CLRAtomic<T> data, T newData, T comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange<T>(ref CLRAtomic<T> data, T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }
        public static int Increment(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            return atomic.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static int Decrement(ref CLRAtomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            return atomic.Decrement(MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange(ref CLRAtomic32 data, int newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }
 
        public static bool CompareExchange(ref CLRAtomic32 data, int newData, int comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic32(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }
 
        public static long Increment(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            return atomic.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static long Decrement(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            return atomic.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Read(ref CLRAtomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            return atomic.Load(MemoryOrder.SequentiallyConsistent); 
        }
        public static void Exchange(ref CLRAtomic64 data, long newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            atomic.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }
        public static bool CompareExchange(ref CLRAtomic64 data, long newData, long comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var atomic = CLRAtomicFactory.GetAtomic64(ref data);
            return atomic.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }
        
        public static void MemoryBarrier([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Fence.Insert(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}