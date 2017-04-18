using System.Runtime.CompilerServices;

namespace RelaSharp.Threading
{      
    class RInterlocked<T>
    {
        // Confirm all MemoryOrder should be seq-cst here...
        public static bool CompareExchange(Atomic<T> data, T newData, T comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange(Atomic<T> data, T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            data.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }

        public static int Increment(Atomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static int Decrement(Atomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Increment(Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static long Decrement(Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Read(Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Exchange or SeqCst read according to memory model? Or ambiguous?
            throw new System.Exception("NICKTODO");
        }
    }
}