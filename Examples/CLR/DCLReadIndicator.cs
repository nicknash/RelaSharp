using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    class DCLReadIndicator : IRelaExample
    {
        class HashedReadIndicator
        {
            private CLRAtomic64[] _occupancyCounts; 
            private int _paddingPower;
            private int _numEntries;

            public HashedReadIndicator(int sizePower, int paddingPower)
            {
                _numEntries = 1 << sizePower;
                int size = _numEntries << paddingPower;
                _occupancyCounts = new CLRAtomic64[size];
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
                    RInterlocked.Read(ref _occupancyCounts[0]);
                    for (int i = 1; i < _numEntries; ++i)
                    {
                        if (RUnordered.Read(ref _occupancyCounts[i << _paddingPower]) > 0)
                        {
                            return true;
                        }
                    }
                    Fence.Insert(MemoryOrder.Release);
                    return false;
                }
            }
        }
        private static TestEnvironment TE = TestEnvironment.TE;
       
        public string Name => "A distributed cache line read indicator.";

        public string Description => "1 writing threads, 2 reading threads";

        public bool ExpectedToFail => false;

        public IReadOnlyList<Action> ThreadEntries { get; }

        private bool _moreConfigurations = true;

        private int _numReading = 0;
        private HashedReadIndicator _readIndicator;
   
        public DCLReadIndicator()
        {
            ThreadEntries = new Action[]{ReadingThread,WritingThread}.ToList();
        }

        public void OnBegin()
        {
        }

        public void OnFinished()
        {
        }

        private void WritingThread()
        {
            int numWrites = 3;
            for(int i = 0; i < numWrites; ++i)
            {
                while(_readIndicator.IsOccupied)
                {
                    TE.Yield();
                }
                TE.Assert(_numReading == 0, $"Write in progress but _numReading is {_numReading}");
            }
        }

        private void ReadingThread()
        {
            int numReads = 3;
            for(int i = 0; i < numReads; ++i)
            {
                _readIndicator.Arrive();
                _numReading++;
                _readIndicator.Depart();
                _numReading--;
            }
        }

        public void PrepareForIteration()
        {
            _numReading = 0;
            _readIndicator = new HashedReadIndicator(4, 3);
            
        }
        public bool SetNextConfiguration()
        {
            var result = _moreConfigurations;
            _moreConfigurations = false;
            return result;
        }
    }
}