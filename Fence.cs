 using System.Runtime.CompilerServices;
 
 namespace RelaSharp
 {
    class Fence
    {
        private static TestEnvironment TE = TestEnvironment.TE;
       
        public static void Insert(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.Fence(mo);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Fence: {mo}");
        }
    }
 }