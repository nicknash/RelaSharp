using System.IO;
using System.Runtime.CompilerServices;

namespace RelaSharp
{ 
    class TestEnvironment // Rename to TestRunner?
    {
        public int HistoryLength => 20;
        public ulong LiveLockLimit = 5000;
        public static TestEnvironment TE = new TestEnvironment();
        public ShadowThread RunningThread => _shadowThreads[_scheduler.RunningThreadId];

        public int NumThreads { get; private set; }
        public bool TestFailed { get; private set; }
        public string TestFailureReason { get; private set;}
        public ulong ExecutionLength { get; private set; }
        public VectorClock SequentiallyConsistentFence { get; private set; }
        
        private RandomScheduler _scheduler;
        private TestThreads _testThreads;
        private ShadowThread[] _shadowThreads;
        private EventLog _eventLog;
        private bool _testStarted;

        public void RunTest(IRelaTest test)
        {
            NumThreads = test.ThreadEntries.Count;
            TestFailed = false;
            _shadowThreads = new ShadowThread[NumThreads];
            _scheduler = new RandomScheduler(NumThreads);
            _eventLog = new EventLog(NumThreads);
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

        private int SchedulingPreamble()
        {          
            if(ExecutionLength > LiveLockLimit)
            {
                FailTest($"Possible live-lock: execution length has exceeded {LiveLockLimit}");
            }
            if(TestFailed)
            {
                throw new TestFailedException();
            }
            ++ExecutionLength;
            return _scheduler.RunningThreadId;
        }

        public void MaybeSwitch()
        {
            if(!_testStarted)
            {
                return;
            }
            int previousThreadId = SchedulingPreamble();
            _scheduler.MaybeSwitch();
            if(_scheduler.RunningThreadId == previousThreadId)
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
            int previousThreadId = SchedulingPreamble();
            if(_scheduler.ThreadWaiting())
            {
                FailTest($"Deadlock detected: all threads waiting.");
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void ThreadFinishedWaiting()
        {
            _scheduler.ThreadFinishedWaiting();
            MaybeSwitch(); // TODO: Do here or by caller?
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

        public void RecordEvent(string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            _eventLog.RecordEvent(RunningThread.Id, RunningThread.Clock, callingThreadFuncName, sourceFilePath, sourceLineNumber, eventInfo);
        }

        public void FailTest(string reason)
        {
            TestFailed = true;
            TestFailureReason = reason;
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
            _eventLog.Dump(output);
            output.WriteLine("----- End Test Execution Log ----");
        }
    }    
}