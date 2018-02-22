using System.Runtime.CompilerServices;

namespace RelaSharp
{
    interface IAtomicInt : IAtomic<int> 
    {
        int Add(int x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        int Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        int Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
    }

    public class AtomicInt : Atomic<int>, IAtomicInt
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        
        public int Add(int x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue + x;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Add ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }

        public int Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue + 1;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Increment ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }

        public int Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var newValue = _memoryOrdered.CurrentValue - 1;
            var oldValue = _memoryOrdered.Exchange(newValue, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Decrement ({mo}): --> {Str(newValue)} ({Str(oldValue)})");
            return newValue;
        }
    }
}

