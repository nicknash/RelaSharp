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
        
        ///<summary>
        /// Intended to mimic the semantics of mprotect (Unix) or FlushProcessWriteBuffers (Windows)
        /// I'm unsure of their exact semantics, but think this is correct. This operation is "fully serializing" in the sense that
        /// all subsequent loads by all threads will see the latest store to every atomic by all other threads before the fence. 
        ///</summary>
        public static void InsertProcessWide([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.MaybeSwitch();
            var seqCstFence = TE.SequentiallyConsistentFence;
            foreach(var thread in TE.AllThreads)
            {
                seqCstFence.Join(thread.ReleasesAcquired);
            }
            foreach(var thread in TE.AllThreads)
            {
                thread.Fence(MemoryOrder.SequentiallyConsistent, seqCstFence);
            }
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, "Fence: ProcessWide");
        }
    }
 }