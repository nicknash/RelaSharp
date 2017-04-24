using System.Runtime.CompilerServices;

namespace RelaSharp.Threading
{      
    class RInterlocked
    {
        // Confirm all MemoryOrder should be seq-cst here...
        // Make use 'ref' parameters even though not required, for uniformity (and no need to construct...too magic?)
        public static bool CompareExchange<T>(ref Atomic<T> data, T newData, T comparand, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.CompareExchange(newData, comparand, MemoryOrder.SequentiallyConsistent); 
        }

        public static void Exchange<T>(ref Atomic<T> data, T newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            data.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }

        public static int Increment(ref Atomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new Atomic32();
            }
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static int Decrement(ref Atomic32 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Increment(ref Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Increment(MemoryOrder.SequentiallyConsistent); 
        }

        public static long Decrement(ref Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return data.Decrement(MemoryOrder.SequentiallyConsistent); 
        }
        public static long Read(ref Atomic64 data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Exchange or SeqCst read according to memory model? Or ambiguous?
            throw new System.Exception("NICKTODO");
        }

        public static void Exchange(ref Atomic64 data, long newData, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            data.Exchange(newData, MemoryOrder.SequentiallyConsistent); 
        }

    }
}