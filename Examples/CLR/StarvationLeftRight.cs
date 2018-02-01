using System;
using System.Collections.Generic;
using System.Threading;
using RelaSharp.CLR;

namespace RelaSharp.Examples.CLR
{
    public class StarvationLeftRight : IRelaExample
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Correct Left-Right implementation that allows writer starvation";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;

        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;
        class ReadIndicator
        {
            private CLRAtomic64 _numReaders;
            public void Arrive()
            {
                RInterlocked.Increment(ref _numReaders);
            }

            public void Depart()
            {
                RInterlocked.Decrement(ref _numReaders);
            }

            public bool IsEmpty => RInterlocked.Read(ref _numReaders) == 0;
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
            private ReadIndicator _readIndicator;
            private CLRAtomic64 _readIndex;
            private InstanceSnoop _snoop = new InstanceSnoop();

            public LeftRightLock()
            {
                _readIndicator = new ReadIndicator();
            }

            public U Read<T, U>(T[] instances, Func<T, U> read)
            {
                _readIndicator.Arrive();
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
                    _readIndicator.Depart();
                }
            }

            public void Write<T>(T[] instances, Action<T> write)
            {
                RMonitor.Enter(_writersMutex);
                try
                {
                    var readIndex = RInterlocked.Read(ref _readIndex);
                    var nextReadIndex = Toggle(readIndex);
                    _snoop.BeginWrite(nextReadIndex);
                    write(instances[nextReadIndex]);
                    _snoop.EndWrite(nextReadIndex);
                    RInterlocked.Exchange(ref _readIndex, nextReadIndex);
                    WaitWhileOccupied(_readIndicator);
                    _snoop.BeginWrite(readIndex);
                    write(instances[readIndex]);
                    _snoop.EndWrite(readIndex);
                }
                finally
                {
                    RMonitor.Exit(_writersMutex);
                }
            }

            private static void WaitWhileOccupied(ReadIndicator readIndicator)
            {
                while (!readIndicator.IsEmpty) ;
            }
            private static long Toggle(long i)
            {
                return i ^ 1;
            }
        }

        private LeftRightLock _lrLock;
        private Dictionary<int, string>[] _instances;
        public StarvationLeftRight()
        {
            ThreadEntries = new List<Action> { ReadThread, ReadThread, WriteThread, WriteThread };
            var configList = new List<SimpleConfig>{new SimpleConfig("Seq-cst operations", MemoryOrder.Relaxed, false)};
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
