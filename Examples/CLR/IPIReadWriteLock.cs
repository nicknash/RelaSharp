using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    public class IPIReadWriteLock : IRelaExample 
    {
        class IPIReadWriteLockInternal 
        {
            private CLRAtomicInt[] _readIndicator; // In a real implementation this would be cache-line padded
            private ThreadLocal<int> _thisReaderIndex;
            private CLRAtomicInt _nextReaderIndex;
            private CLRAtomicInt _writerActive;
            public IPIReadWriteLockInternal(int numReaders)
            {
                _readIndicator = new CLRAtomicInt[numReaders + 1];
                _thisReaderIndex = new ThreadLocal<int>();
            }

            private int GetReaderIndex()
            {
                if(_thisReaderIndex.IsValueCreated)
                {
                    return _thisReaderIndex.Value;
                }
                var result = RInterlocked.Increment(ref _nextReaderIndex) - 1;
                _thisReaderIndex.Value = result;
                return result;
            }

            internal void EnterReadLock()
            {
                var idx = GetReaderIndex();
                RUnordered.Write(ref _readIndicator[idx], 1);
                if(RUnordered.Read(ref _writerActive) == 1)
                {
                    RUnordered.Write(ref _readIndicator[idx], 0);
                    //Console.WriteLine($"reader no longer trying to enter at {idx}");
                    RMonitor.Enter(_lockObj);
                    RUnordered.Write(ref _readIndicator[idx], 1);
                    RMonitor.Exit(_lockObj);
                }
            }    
  
            internal void EnterWriteLock()
            {
                RMonitor.Enter(_lockObj); 
                RUnordered.Write(ref _writerActive, 1);
                RInterlocked.MemoryBarrierProcessWide();
                while (ReadInProgress())
                {
                    RE.Yield();
                }
                RMonitor.Exit(_lockObj);
                return;
            }

            private bool ReadInProgress()
            {
                for(int i = 0; i < _readIndicator.Length; ++i)
                {
                    if(RUnordered.Read(ref _readIndicator[i]) != 0)
                    {
                        return true;
                    }
                }
                return false;
            }

            private object _lockObj = new object();

            internal void ExitWriteLock()
            {
                RUnordered.Write(ref _writerActive, 0);
            }

            internal void ExitReadLock()
            {
                RUnordered.Write(ref _readIndicator[_thisReaderIndex.Value], 0);
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Read-write lock via interprocessor interrupt";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => true; // TODO: Revise example, currently fails to enforce mutual exclusion. 
        private static IRelaEngine RE = RelaEngine.RE;
        private int _threadsPassed;
        private bool _hasRun;
        private IPIReadWriteLockInternal _rwLock;

        public IPIReadWriteLock()
        {
            ThreadEntries = new List<Action> {Reader,Writer};
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Reader()
        {
            for (int i = 0; i < 2; ++i)
            {
                _rwLock.EnterReadLock(); 
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread0 entered while Thread1 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                _rwLock.ExitReadLock();
                _threadsPassed--;
            }
        }

        public void Writer()
        {
            for (int i = 0; i < 2; ++i)
            {
                _rwLock.EnterWriteLock();
                RE.Assert(_threadsPassed == 0, $"Iteration {i}: Thread1 entered while Thread0 in critical section! ({_threadsPassed})");
                _threadsPassed++;
                _rwLock.ExitWriteLock();
                _threadsPassed--;
            }
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            _threadsPassed = 0;
            _rwLock = new IPIReadWriteLockInternal(1);
        }

        public bool SetNextConfiguration()
        {
            bool oldHasRun = _hasRun;
            PrepareForNewConfig();
            _hasRun = true;
            return !oldHasRun;
        }
    }
}
