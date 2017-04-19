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
        private Thread[] _threads;
        private ThreadState[] _threadStates;
        private ShadowThread[] _shadowThreads;
        private Object[] _threadLocks;
        private List<ExecutionEvent> _eventLog;
        private object _runningThreadLock = new object();
        private bool _testStarted;
        public  VectorClock SequentiallyConsistentFence;

        private void MakeThreadFunction(Action threadFunction, int threadIdx)
        {
            var l = _threadLocks[threadIdx]; 
            
            lock(l)
            {
                _threadStates[threadIdx] = ThreadState.Blocked;
                Monitor.Pulse(l);                
                Monitor.Wait(l);
            }
            try
            {
                Monitor.Enter(_runningThreadLock);
                threadFunction();
            }
            catch(TestFailedException)
            {
            }
            Monitor.Exit(_runningThreadLock);
            _threadStates[threadIdx] = ThreadState.Finished;
            _scheduler.ThreadFinished(threadIdx);
            if(!_scheduler.AllThreadsFinished)
            {
                _scheduler.MaybeSwitch();
                WakeThread();
            }
        }

        // private MakeOnBeginThread(Action onBegin)
        // {
        //     MakeThreadFunction()
        // }

        public void RunTest(IRelaTest test)
        {
            NumThreads = test.ThreadEntries.Count;
            TestFailed = false;
            _threads = new Thread[NumThreads];
            _threadStates = new ThreadState[NumThreads];
            _threadLocks = new Object[NumThreads];
            _shadowThreads = new ShadowThread[NumThreads];
            _scheduler = new RandomScheduler(NumThreads);
            _eventLog = new List<ExecutionEvent>();
            _testStarted = false;
            SequentiallyConsistentFence = new VectorClock(NumThreads);
            ExecutionLength = 0;
            
            for(int i = 0; i < NumThreads; ++i)
            {
                _threadLocks[i] = new Object();
                int j = i;
                Action threadEntry = test.ThreadEntries[j];
                Action wrapped = () => MakeThreadFunction(threadEntry, j); 
                _threadStates[i] = ThreadState.Running;
                _threads[i] = new Thread(new ThreadStart(wrapped));
                _threads[i].Start();
                _shadowThreads[i] = new ShadowThread(i, NumThreads);
            }
            for(int i = 0; i < NumThreads; ++i)
            {
                var l = _threadLocks[i];
                lock(l)
                {
                    while(_threadStates[i] == ThreadState.Running)
                    {
                        Monitor.Wait(l);
                    }
                }
            }    
            test.OnBegin();
            _testStarted = true;   
            WakeThread();
            for(int i = 0; i < NumThreads; ++i)
            {
                _threads[i].Join();
            }
            test.OnFinished();
        }

        private void WakeThread()
        {
            int idx = _scheduler.RunningThreadId;
            _threadStates[idx] = ThreadState.Running;
            var l = _threadLocks[idx];
            lock(l)
            {
                Monitor.Pulse(l);
            }
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

        private void WakeThreadAndBlock(int previousThreadId)
        {
            _threadStates[previousThreadId] = ThreadState.Blocked;
            WakeThread();             
            var runningLock = _threadLocks[previousThreadId];
            lock(runningLock)
            {
                Monitor.Exit(_runningThreadLock);
                while(_threadStates[previousThreadId] == ThreadState.Blocked)
                {
                    Monitor.Wait(runningLock);
                }
            }
            Monitor.Enter(_runningThreadLock);        
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
            WakeThreadAndBlock(previousThreadId);        
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
            WakeThreadAndBlock(previousThreadId);        
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