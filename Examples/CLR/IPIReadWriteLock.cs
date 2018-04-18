using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    public class IPIReadWriteLock : IRelaExample 
    {
        class Snoop
        {
            private int _numReaders = 0;
            private bool _writeInProgress = false;
            public void BeginRead()
            {
                _numReaders++;
                RE.Assert(!_writeInProgress, $"Write in progress with {_numReaders} readers!");
            }

            public void EndRead()
            {
                _numReaders--;
            }

            public void BeginWrite()
            {
                _writeInProgress = true;
                RE.Assert(_numReaders == 0, $"Write in progress with {_numReaders} readers!");
            }

            public void EndWrite()
            {
                _writeInProgress = false;
            }
        }
        class IPIReadWriteLockInternal 
        {
            private CLRAtomicInt[] _readIndicator; // In a real implementation this would be cache-line padded
            private ThreadLocal<int> _thisReaderIndex;
            private CLRAtomicInt _nextReaderIndex;
            private CLRAtomicInt _writerActive;
            private Snoop _snoop = new Snoop();
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
                    RMonitor.Enter(_lockObj);
                    RUnordered.Write(ref _readIndicator[idx], 1);
                    RMonitor.Exit(_lockObj);
                }
                _snoop.BeginRead();
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
                _snoop.BeginWrite();
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
                _snoop.EndWrite();
                RMonitor.Exit(_lockObj);
            }

            internal void ExitReadLock()
            {
                RUnordered.Write(ref _readIndicator[_thisReaderIndex.Value], 0);
                _snoop.EndRead();
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Read-write lock via interprocessor interrupt";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide";
        public bool ExpectedToFail => false;
        private static IRelaEngine RE = RelaEngine.RE;
        private bool _hasRun;
        private IPIReadWriteLockInternal _rwLock;

        public IPIReadWriteLock()
        {
            ThreadEntries = new List<Action> {Reader,Reader,Writer};
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
                _rwLock.ExitReadLock();
            }
        }

        public void Writer()
        {
            for (int i = 0; i < 2; ++i)
            {
                _rwLock.EnterWriteLock();
                _rwLock.ExitWriteLock();
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
            _rwLock = new IPIReadWriteLockInternal(2);
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
