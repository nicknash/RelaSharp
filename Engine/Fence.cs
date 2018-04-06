 using System.Runtime.CompilerServices;
 
 namespace RelaSharp
 {
    public class Fence
    {
        private static TestEnvironment TE = TestEnvironment.TE;
       
        public static void Insert(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            runningThread.Fence(mo, TE.SequentiallyConsistentFence);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Fence: {mo}");
        }

        public static void InsertProcessWide([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.MaybeSwitch();
            foreach(var thread in TE.AllThreads)
            {
                thread.Fence(MemoryOrder.SequentiallyConsistent, TE.SequentiallyConsistentFence);
            }
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, "Fence: ProcessWide");
        }
    }
 }