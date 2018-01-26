using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    class SingleCounterReadIndicator : IRelaExample
    {
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

        private static TestEnvironment TE = TestEnvironment.TE;
       
        public string Name => "A read-indicator implemented as single counter";

        public string Description => "1 writing threads, 2 reading threads";

        public bool ExpectedToFail => false;

        public IReadOnlyList<Action> ThreadEntries { get; }

        private bool _moreConfigurations = true;

        private int _numReading = 0;
        //private int _numWriting = 0;
        private ReadIndicator _readIndicator;
   
        public SingleCounterReadIndicator()
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
                while(!_readIndicator.IsEmpty)
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
            _numReading = 0;//_numWriting = 0;
            _readIndicator = new ReadIndicator();
            
        }
        public bool SetNextConfiguration()
        {
            var result = _moreConfigurations;
            _moreConfigurations = false;
            return result;
        }
    }
}