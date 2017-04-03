 namespace RelaSharp
 {
    class ExecutionEvent 
    {
        public readonly int ThreadId;
        public readonly long ThreadClock;
        public readonly string CallingMemberName;
        public readonly string SourceFilePath;
        public readonly int SourceLineNumber;
        public readonly string EventInfo;

        public ExecutionEvent(int threadId, long threadClock, string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            ThreadId = threadId;
            ThreadClock = threadClock;
            CallingMemberName = callingThreadFuncName;
            SourceFilePath = sourceFilePath;
            SourceLineNumber = sourceLineNumber;
            EventInfo = eventInfo;
        }
    }
 }