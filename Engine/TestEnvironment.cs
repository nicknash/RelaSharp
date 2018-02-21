using System.IO;
using System.Runtime.CompilerServices;
using RelaSharp.Scheduling;
using RelaSharp.MemoryModel;

namespace RelaSharp
{ 
    enum EngineMode 
    {
        Test,
        LiveAssert,
        LiveNoAssert        
    }

    public interface IRelaEngine 
    {
        void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Yield();
    }

    public class TestRunner 
    {
        public static TestRunner TR = new TestRunner();

        private static TestEnvironment TE = TestEnvironment.TE; 
        public void RunTest(IRelaTest test, IScheduler scheduler, ulong liveLockLimit)
         => TE.RunTest(test, scheduler, liveLockLimit);

        public bool TestFailed => TE.TestFailed;

        public ulong ExecutionLength => TE.ExecutionLength;
    
        public void DumpExecutionLog(TextWriter output)
         => TE.DumpExecutionLog(output);

    }


    public class RelaEngine
    {
        public static RelaEngine RE = new RelaEngine();

        private static TestEnvironment TE = TestEnvironment.TE; 
        public void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
         => TE.Assert(shouldBeTrue, reason, memberName, sourceFilePath, sourceLineNumber);
        public void Yield() 
         => TE.Yield();
        public void MaybeSwitch()
         => TE.MaybeSwitch();
    }

    class TestEnvironment
    {
        public int HistoryLength => 20;
        public ulong LiveLockLimit { get; private set;}
        public static TestEnvironment TE = new TestEnvironment();
        public ShadowThread RunningThread => _shadowThreads[_scheduler.RunningThreadId];

        public int NumThreads { get; private set; }
        public bool TestFailed { get; private set; }
        public string TestFailureReason { get; private set;}
        public ulong ExecutionLength { get; private set; }
        public VectorClock SequentiallyConsistentFence { get; private set; }
        public ILookback Lookback => _scheduler;
        private IScheduler _scheduler;
        private TestThreads _testThreads;
        private ShadowThread[] _shadowThreads;
        private EventLog _eventLog;
        private bool _testStarted;

        public void RunTest(IRelaTest test, IScheduler scheduler, ulong liveLockLimit)
        {
            NumThreads = test.ThreadEntries.Count;
            _scheduler = scheduler;
            LiveLockLimit = liveLockLimit;
            TestFailed = false;
            _shadowThreads = new ShadowThread[NumThreads];
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
                return; // This happens due to OnBegin running before the thread starts (on some default thread)
            }
            int previousThreadId = SchedulingPreamble();
            _scheduler.MaybeSwitch();
            if(_scheduler.RunningThreadId == previousThreadId)
            {
                return;
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void ThreadWaiting(int waitingOnThreadId, object lockObject)
        {
            if(!_testStarted)
            {
                return;
            }
            int previousThreadId = SchedulingPreamble();
            if(_scheduler.ThreadWaiting(waitingOnThreadId, lockObject))
            {
                FailTest($"Deadlock detected: all threads waiting.");
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void ThreadFinishedWaiting()
        {
            _scheduler.ThreadFinishedWaiting();
        }

        public void Yield()
        {
            int previousThreadId = SchedulingPreamble();
            _scheduler.Yield();
            if(_scheduler.RunningThreadId == previousThreadId)
            {
                return;
            }
            _testThreads.WakeNewThreadAndBlockPrevious(previousThreadId);        
        }

        public void LockReleased(object lockObject)
        {
            _scheduler.LockReleased(lockObject);
        }

        public void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string assertResult = shouldBeTrue ? "passed" : "failed";
            RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Assert ({assertResult}): {reason}");
            if(!shouldBeTrue)
            {
                FailTest(TestFailed ? $"{TestFailureReason} AND {reason}" : reason);
            }
        }

        public void RecordEvent(string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            _eventLog.RecordEvent(RunningThread.Id, RunningThread.Clock, callingThreadFuncName, sourceFilePath, sourceLineNumber, eventInfo);
        }

        public void FailTest(string reason)
        {
            TestFailureReason = reason;
            TestFailed = true;
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