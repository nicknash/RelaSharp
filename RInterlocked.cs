using System.Runtime.CompilerServices;

namespace RelaSharp.Examples
{      
    class RInterlocked<T>
    {
        // Confirm all MemoryOrder should be seq-cst here...
        public static bool CompareExchange(MemoryOrdered<T> data, T newData, T comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange(MemoryOrdered<T> data, T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            data.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }

        public static int Increment(MemoryOrderedInt32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static int Decrement(MemoryOrderedInt32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Increment(MemoryOrderedInt64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static long Decrement(MemoryOrderedInt64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Read(MemoryOrderedInt64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Exchange or SeqCst read according to memory model? Or ambiguous?
            throw new System.Exception("NICKTODO");
        }
    }
}