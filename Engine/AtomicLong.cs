using System.Runtime.CompilerServices;

namespace RelaSharp
{
    interface IAtomicLong : IAtomic<long> 
    {
        long Add(long x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        long Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        long Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
    }

    public class AtomicLong : Atomic<long>, IAtomicLong
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        
        public long Add(long x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue + x;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Add ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }

        public long Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue + 1;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Increment ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }

        public long Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue - 1;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Decrement ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }
    }
}

