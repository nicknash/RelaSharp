using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.CLR;

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
            private CLRAtomic32[] _occupancyCounts; 
            private int _paddingPower;
            private int _numEntries;

            public HashedReadIndicator(int sizePower, int paddingPower)
            {
                _numEntries = 1 << sizePower;
                
                int size = _numEntries << paddingPower;
                _occupancyCounts = new CLRAtomic32[size];
                _paddingPower = paddingPower;
            }

            private int GetIndex()
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                var result = (threadId.GetHashCode() & (_numEntries - 1)) << _paddingPower;
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
                    for (int i = 0; i < _numEntries; ++i)
                    {
                        if (RUnordered.Read(ref _occupancyCounts[i << _paddingPower]) > 0)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        class InstanceSnoop
        {
            private static TestEnvironment TE = TestEnvironment.TE;

            private HashSet<long> _reading = new HashSet<long>();
            private HashSet<long> _writing = new HashSet<long>();

            public void BeginRead(long which)
            {
                TE.MaybeSwitch();
                TE.Assert(!_writing.Contains(which), $"Write in progress during read at {which}");
                _reading.Add(which);
            }

            public void EndRead(long which)
            {
                TE.MaybeSwitch();                
                _reading.Remove(which);
                TE.Assert(!_writing.Contains(which), $"Write in progress during read at {which}");
            }

            public void BeginWrite(long which)
            {
                TE.MaybeSwitch();                
                TE.Assert(!_reading.Contains(which), $"Read in progress during write at {which}");
                TE.Assert(!_writing.Contains(which), $"Write in progress during write at {which}");
                _writing.Add(which);
            }

            public void EndWrite(long which)
            {
                TE.MaybeSwitch();                
                TE.Assert(!_reading.Contains(which), $"Write in progress during read at {which}");
                _writing.Remove(which);
            }
        }

        class LeftRightLock
        {
            private readonly Object _writersMutex = new Object();
            private HashedReadIndicator[] _readIndicator;
            private CLRAtomic64 _versionIndex;
            private CLRAtomic64 _readIndex;
            private InstanceSnoop _snoop = new InstanceSnoop();

            public LeftRightLock()
            {
                _readIndicator = new HashedReadIndicator[2];
                _readIndicator[0] = new HashedReadIndicator(3, 1);
                _readIndicator[1] = new HashedReadIndicator(3, 1);
            }

            public U Read<T, U>(T[] instances, Func<T, U> read)
            {
                var versionIndex = RUnordered.Read(ref _versionIndex);
                var readIndicator = _readIndicator[versionIndex];
                readIndicator.Arrive();
                try
                {   
                    var idx = RInterlocked.Read(ref _readIndex);
                    _snoop.BeginRead(idx);
                    var result = read(instances[idx]);
                    _snoop.EndRead(idx);
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
                    var readIndex = RUnordered.Read(ref _readIndex);
                    var nextReadIndex = Toggle(readIndex);
                    _snoop.BeginWrite(nextReadIndex);
                    write(instances[nextReadIndex]);
                    _snoop.EndWrite(nextReadIndex);
                    
                    // Move subsequent readers to 'next' instance 
                    RInterlocked.Exchange(ref _readIndex, nextReadIndex);
                    // Wait for all readers marked in the 'next' read indicator,
                    // these readers could be reading the 'readIndex' instance 
                    // we want to write next 
                    //var versionIndex = RVolatile.Read(ref _versionIndex);
                    var versionIndex = RUnordered.Read(ref _versionIndex);
                    var nextVersionIndex = Toggle(versionIndex);

                    WaitWhileOccupied(_readIndicator[nextVersionIndex]);
                    // Move subsequent readers to the 'next' read indicator 
                    RUnordered.Write(ref _versionIndex, nextVersionIndex);
                    // At this point all subsequent readers will read the 'next' instance
                    // and mark the 'nextVersionIndex' read indicator, so the only remaining potential
                    // readers are the ones on the 'versionIndex' read indicator, so wait for them to finish 
                    WaitWhileOccupied(_readIndicator[versionIndex]);
                    // At this point there may be readers, but they must be on nextReadIndex, we can 
                    // safely write.
                    _snoop.BeginWrite(readIndex);
                    write(instances[readIndex]);
                    _snoop.EndWrite(readIndex);
                }
                finally
                {
                    RMonitor.Exit(_writersMutex);
                }
            }

            private static void WaitWhileOccupied(HashedReadIndicator readIndicator)
            {
                while (readIndicator.IsOccupied) ;
            }
            private static long Toggle(long i)
            {
                return i ^ 1;
            }
        }

        private LeftRightLock _lrLock;
        private Dictionary<int, string>[] _instances;
        public LeftRight()
        {
            ThreadEntries = new List<Action> { ReadThread, ReadThread, WriteThread, WriteThread };
            var configList = new List<SimpleConfig>{new SimpleConfig("Mixed operations", MemoryOrder.Relaxed, false)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void ReadThread()
        {
            for(int i = 0; i < 5; ++i)
            {
                string message = null;
                bool read = _lrLock.Read(_instances, d => d.TryGetValue(i, out message));
            }
        }

        public void WriteThread()
        {
            for(int i = 0; i < 5; ++i)
            {
                _lrLock.Write(_instances, d => d[i] = $"Wrote This: {i}");
            }
        }
        public void OnBegin()
        {
        }
        public void OnFinished()
        {

        }
        private void SetupActiveConfig()
        {
        }

        private void PrepareForNewConfig()
        {
            _lrLock = new LeftRightLock();
            _instances = new Dictionary<int, string>[2];
            _instances[0] = new Dictionary<int, string>();
            _instances[1] = new Dictionary<int, string>();
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            if (ActiveConfig != null)
            {
                SetupActiveConfig();
            }
            return moreConfigurations;
        }

    }
}
