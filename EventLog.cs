using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RelaSharp
{
    class EventLog 
    {
        private List<ExecutionEvent> _eventLog;
        private int _numThreads;

        public EventLog(int numThreads)
        {
            _numThreads = numThreads;
            _eventLog = new List<ExecutionEvent>(10000);
        }

        public void RecordEvent(int runningThreadId, long runningThreadClock, string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            _eventLog.Add(new ExecutionEvent(runningThreadId, runningThreadClock, callingThreadFuncName, sourceFilePath, sourceLineNumber, eventInfo));
        }

        public void Dump(TextWriter output)
        {
           var directories = _eventLog.Select(e => Path.GetDirectoryName(e.SourceFilePath)).Distinct();
            output.WriteLine();            
            output.WriteLine($"Code executed in directories: {string.Join(",", directories)}");
            output.WriteLine();
            output.WriteLine("Interleaved execution log");
            output.WriteLine("*************************");
            for(int i = 0; i < _eventLog.Count; ++i)
            {
                var e = _eventLog[i];
                var fileName = Path.GetFileName(e.SourceFilePath);
                output.WriteLine($"[{i}] {e.ThreadId}@{e.ThreadClock} in {fileName}:{e.CallingMemberName}:{e.SourceLineNumber} | {e.EventInfo}");
            }
            output.WriteLine();        
            output.WriteLine("Individual thread logs");
            output.WriteLine("**********************");
            for(int i = 0; i < _numThreads; ++i)
            {
                output.WriteLine($"Thread {i}");
                output.WriteLine("--------");
                for(int j = 0; j < _eventLog.Count; ++j)
                {
                    var e = _eventLog[j];
                    if(e.ThreadId == i)
                    {
                        var fileName = Path.GetFileName(e.SourceFilePath);
                        output.WriteLine($"[{j}] {e.ThreadId}@{e.ThreadClock} in {fileName}:{e.CallingMemberName}:{e.SourceLineNumber} | {e.EventInfo}");
                    }
                }
            }
        }

    }
    
}