using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.Threading;

namespace RelaSharp.Examples.CLR
{
    public class LeftRight : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Left-Right Synchronization Primitive Example";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;

        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

    class HashedReadIndicator 
    {
        private Atomic32[] _occupancyCounts;
        private int _paddingPower;

        public HashedReadIndicator(int numEntries, int paddingPower)
        {
            _occupancyCounts = new Atomic32[numEntries << paddingPower];
            _paddingPower = paddingPower;
        }

        private int GetIndex()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId; 
            var result = threadId.GetHashCode() << _paddingPower;
            return result;
        }
        
        public void Arrive()
        {
            int index = GetIndex();
            RInterlocked.Increment(ref _occupancyCounts[index]);
        }

        public void Depart()
        {
            int index = GetIndex();
            RInterlocked.Decrement(ref _occupancyCounts[index]);
        }

        public bool IsOccupied
        {
            get
            {
                // TODO: Memory fencing!
                for (int i = 0; i < _occupancyCounts.Length; ++i)
                {
                    if (_occupancyCounts[i << _paddingPower] > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }


        class LeftRightLock
        {
            private readonly Object _writersMutex = new Object();
            private HashedReadIndicator[] _readIndicator;
            private Atomic64 _versionIndex;
            private Atomic64 _readIndex;

            public LeftRightLock()
            {
                _readIndicator = new HashedReadIndicator[2];
                _readIndicator[0] = new HashedReadIndicator(2, 1);
                _readIndicator[1] = new HashedReadIndicator(2, 1);
            }

            public U Read<T, U>(T[] instances, Func<T, U> read)
            {
                var versionIndex = _versionIndex.Load(MemoryOrder.SequentiallyConsistent);
                var readIndicator = _readIndicator[versionIndex];
                readIndicator.Arrive();
                try
                {
                    var result = read(instances[_readIndex.Load(MemoryOrder.SequentiallyConsistent)]);
                    return result;
                }
                finally
                {
                    readIndicator.Depart();
                }
            }

            public void Write<T>(T[] instances, Action<T> write)
            {
                RMonitor.Enter(_writersMutex);
                try
                {
                    var readIndex = RInterlocked.Read(ref _readIndex);
                    var nextReadIndex = Toggle(readIndex);
                    write(instances[nextReadIndex]);
                    // Move subsequent readers to 'next' instance 
                    WriteSeqCst(ref _readIndex, nextReadIndex);
                    _readIndex.Store(nextReadIndex, MemoryOrder.SequentiallyConsistent);
                    // Wait for all readers marked in the 'next' read indicator,
                    // these readers could be reading the 'readIndex' instance 
                    // we want to write next 
                    var versionIndex = _versionIndex.Load(MemoryOrder.SequentiallyConsistent);
                    var nextVersionIndex = Toggle(versionIndex);
                    
                    WaitWhileOccupied(_readIndicator[nextVersionIndex]);
                    // Move subsequent readers to the 'next' read indicator 
                    _versionIndex.Store(nextVersionIndex, MemoryOrder.SequentiallyConsistent);
                    // At this point all readers subsequent readers will read the 'next' instance
                    // and mark the 'nextVersionIndex' read indicator, so the only remaining potential
                    // readers are the ones on the 'versionIndex' read indicator, so wait for them to finish 
                    WaitWhileOccupied(_readIndicator[versionIndex]);
                    // At this point there may be readers, but they must be on nextReadIndex, we can 
                    // safely write.
                    write(instances[readIndex]);
                }
                finally
                {
                    RMonitor.Exit(_writersMutex);
                }
            }

            private void WriteSeqCst(ref Atomic64 a, long newValue)
            {
                // TODO: API wart here currently.
                RInterlocked.Exchange(ref a, newValue);
            }

            private static void WaitWhileOccupied(HashedReadIndicator readIndicator)
            {
                while(readIndicator.IsOccupied) ;
            }
            private static long Toggle(long i)
            {
                return i ^ 1;
            }
        }

        public LeftRight()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("All operations relaxed", MemoryOrder.Relaxed, true), 
                                                    new SimpleConfig("All operations acquire-release", MemoryOrder.AcquireRelease, true),
                                                    new SimpleConfig("All operations sequentially consistent", MemoryOrder.SequentiallyConsistent, false)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
        }

        public void Thread2()
        {
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
}
