using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace RelaSharp
{
    enum ThreadState 
    {
        Running,
        Blocked,
        Finished
    }
    
    class TestFailedException : Exception 
    {

    }

    class TestEnvironment
    {
        public static TestEnvironment TE = new TestEnvironment();
        public ShadowThread RunningThread => _shadowThreads[_runningThreadIdx];
        public int NumThreads { get; private set; }
        public int HistoryLength => 20;
        public bool TestFailed { get; private set; }
        public string TestFailureReason { get; private set;}
        private int _runningThreadIdx;
        private int[] _unfinishedThreadIndices;
        private int _numUnfinishedThreads; // TODO: wrap these two into a proper data structure when shape clearer.
        private Thread[] _threads;
        private ThreadState[] _threadStates;
        private ShadowThread[] _shadowThreads;
        private Object[] _threadLocks;
        private Random _random = new Random();
        private List<ExecutionEvent> _eventLog;
        private object _runningThreadLock = new object();

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
            if(_numUnfinishedThreads > 1)
            {
                int i = Array.IndexOf(_unfinishedThreadIndices, threadIdx);
                _unfinishedThreadIndices[i] = _unfinishedThreadIndices[_numUnfinishedThreads - 1];
                _numUnfinishedThreads--;
                int nextIdx = GetNextThreadIdx(); 
                WakeThread(nextIdx);
            }
        }

        public void RunTest(ITest test)
        {
            NumThreads = test.ThreadEntries.Count;
            TestFailed = false;
            _threads = new Thread[NumThreads];
            _threadStates = new ThreadState[NumThreads];
            _threadLocks = new Object[NumThreads];
            _shadowThreads = new ShadowThread[NumThreads];
            _unfinishedThreadIndices = new int[NumThreads];
            _numUnfinishedThreads = NumThreads;
            _eventLog = new List<ExecutionEvent>();
            
            for(int i = 0; i < NumThreads; ++i)
            {
                _unfinishedThreadIndices[i] = i;
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
            WakeThread(GetNextThreadIdx());
            for(int i = 0; i < NumThreads; ++i)
            {
                _threads[i].Join();
            }
            test.OnFinished();
        }

        private void WakeThread(int idx)
        {
            _runningThreadIdx = idx;
            _threadStates[idx] = ThreadState.Running;
            var l = _threadLocks[idx];
            lock(l)
            {
                Monitor.Pulse(l);
            }
        }
        public void Scheduler()
        {
            if(TestFailed)
            {
                throw new TestFailedException();
            }
            int prevThreadIdx = _runningThreadIdx;
            int nextThreadIdx = GetNextThreadIdx();
            if(nextThreadIdx == prevThreadIdx)
            {
                return;
            }
            _threadStates[prevThreadIdx] = ThreadState.Blocked;
            WakeThread(nextThreadIdx);             
            var runningLock = _threadLocks[prevThreadIdx];
            lock(runningLock)
            {
                Monitor.Exit(_runningThreadLock);
                while(_threadStates[prevThreadIdx] == ThreadState.Blocked) // I may get here, and be woken before I got a chance to sleep. That's OK. 
                {
                    Monitor.Wait(runningLock);
                }
            }
            Monitor.Enter(_runningThreadLock);        
        }

        public void Assert(bool shouldBeTrue, string reason)
        {
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


        private int GetNextThreadIdx()
        {
            if(_numUnfinishedThreads == 0)
            {
                throw new Exception("All threads finished. Who called?");
            }
            return _unfinishedThreadIndices[_random.Next(_numUnfinishedThreads)];
        }
    }    
}