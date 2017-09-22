using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.CLR;

namespace RelaSharp.Examples.CLR
{
    public class NaiveLeftRight : IRelaExample
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Naive Left-Right Synchronization Primitive Example";
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
            private CLRAtomic64 _index;
            private InstanceSnoop _snoop = new InstanceSnoop();

            public LeftRightLock()
            {
                _readIndicator = new HashedReadIndicator[2];
                _readIndicator[0] = new HashedReadIndicator(3, 1);
                _readIndicator[1] = new HashedReadIndicator(3, 1);
            }

            public U Read<T, U>(T[] instances, Func<T, U> read)
            {
                var index = RInterlocked.Read(ref _index);
                var readIndicator = _readIndicator[index];
                readIndicator.Arrive();
                try
                {
                    _snoop.BeginRead(index);
                    var result = read(instances[index]);
                    _snoop.EndRead(index);
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
                    var index = RInterlocked.Read(ref _index);
                    var nextIndex = Toggle(index);
                    WaitWhileOccupied(_readIndicator[nextIndex]); // Now we're subject to starvation by readers.

                    _snoop.BeginWrite(nextIndex);
                    write(instances[nextIndex]);
                    _snoop.EndWrite(nextIndex);
                    
                    // Move subsequent readers to 'next' instance 
                    RInterlocked.Exchange(ref _index, nextIndex);
                    // Wait for all readers to finish reading the instance we want to write next
                    WaitWhileOccupied(_readIndicator[index]);
                    // At this point there may be readers, but they must be on nextReadIndex, we can 
                    // safely write.
                    _snoop.BeginWrite(index);
                    write(instances[index]);
                    _snoop.EndWrite(index);
                }
                finally
                {
                    RMonitor.Exit(_writersMutex);
                }
            }

            private static void WaitWhileOccupied(HashedReadIndicator readIndicator)
            {
                while (readIndicator.IsOccupied) TE.Yield();
            }
            private static long Toggle(long i)
            {
                return i ^ 1;
            }
        }

        private LeftRightLock _lrLock;
        private Dictionary<int, string>[] _instances;


        public NaiveLeftRight()
        {
            ThreadEntries = new List<Action> { ReadThread, WriteThread };
            var configList = new List<SimpleConfig>{new SimpleConfig("Mixed operations", MemoryOrder.Relaxed, false)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void ReadThread()
        {
            for(int i = 0; i < 1; ++i)
            {
                string message = null;
                bool read = _lrLock.Read(_instances, d => d.TryGetValue(i, out message));
            }
        }

        public void WriteThread()
        {
            for(int i = 0; i < 1; ++i)
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
