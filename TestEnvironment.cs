using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    enum ThreadState 
    {
        Running,
        Blocked,
        Waiting,
        Finished
    }
    
 
    class TestFailedException : Exception 
    {

    }

    

    class TestEnvironment // Rename to TestRunner when scheduling removed?
    {
        public static TestEnvironment TE = new TestEnvironment();
        public ShadowThread RunningThread => _shadowThreads[_scheduler.RunningThreadId];
        public int NumThreads { get; private set; }
        public int HistoryLength => 20;
        public ulong LiveLockLimit = 5000;
        public bool TestFailed { get; private set; }
        public string TestFailureReason { get; private set;}
        public ulong ExecutionLength { get; private set; }

        private RandomScheduler _scheduler;
        private TestThreads _testThreads;
        private ShadowThread[] _shadowThreads;
        private List<ExecutionEvent> _eventLog;
        private bool _testStarted;
        public  VectorClock SequentiallyConsistentFence;

        public void RunTest(IRelaTest test)
        {
            NumThreads = test.ThreadEntries.Count;
            TestFailed = false;
            _shadowThreads = new ShadowThread[NumThreads];
            _scheduler = new RandomScheduler(NumThreads);
            _eventLog = new List<ExecutionEvent>();
            _testStarted = false;
            SequentiallyConsistentFence = new VectorClock(NumThreads);
            ExecutionLength = 0;
            
            for(int i = 0; i < NumThreads; ++i)
            {
                _shadowThreads[i] = new ShadowThread(i, NumThreads);
            }

            _testThreads = new TestThreads(test, _scheduler);
            test.OnBegin();
            _testStarted = true;   
            _testThreads.WakeThread();
            _testThreads.Join();
            test.OnFinished();
        }

        private void SchedulingPreamble()
        {          
            if(ExecutionLength > LiveLockLimit)
            {
                FailTest($"Possible live-lock: execution length has exceeded {LiveLockLimit}");
            }
            if(TestFailed)
            {
                throw new TestFailedException();
            }
        }

        public void MaybeSwitch()
        {
            if(!_testStarted)
            {
                return;
            }
            SchedulingPreamble();
            ++ExecutionLength;
            int previousThreadId = _scheduler.RunningThreadId;
            int nextThreadId = _scheduler.MaybeSwitch();
            if(nextThreadId == previousThreadId)
            {
                return;
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void ThreadWaiting()
        {
           if(!_testStarted)
            {
                return;
            }
            SchedulingPreamble();
            ++ExecutionLength;
            int previousThreadId = _scheduler.RunningThreadId;
            if(_scheduler.ThreadWaiting())
            {
                FailTest("DEADLOCK");
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string assertResult = shouldBeTrue ? "passed" : "failed";
            RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Assert ({assertResult}): {reason}");
            if(!shouldBeTrue)
            {
                FailTest(reason);
            }
        }

        public void FailTest(string reason)
        {
            TestFailed = true;
            TestFailureReason = reason;
        }

        public void RecordEvent(string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            _eventLog.Add(new ExecutionEvent(RunningThread.Id, RunningThread.Clock, callingThreadFuncName, sourceFilePath, sourceLineNumber, eventInfo));
        }

        public void DumpExecutionLog(TextWriter output)
        {
            output.WriteLine("----- Begin Test Execution Log ----");
            output.WriteLine();
            if(TestFailed)
            {
                output.WriteLine($"Test failed with reason: '{TestFailureReason}'");
            }
            else
            {
                output.WriteLine($"Test passed.");
            }
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
            for(int i = 0; i < NumThreads; ++i)
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
            output.WriteLine("----- End Test Execution Log ----");
        }
    }    
}