using System;

namespace RelaSharp.Scheduling
{
    class NaiveRandomScheduling : ISchedulingAlgorithm
    {
        private readonly Random _random = new Random();
        private readonly int _numIterations;
        private int _iterationCount;

        public NaiveRandomScheduling(int numIterations)
        {
            _numIterations = numIterations;
            _iterationCount = 0;
        }

        public int GetDifferentThreadIndex(int runningThreadIndex, int numUnfinishedThreads)
        {
            int idx = GetNextThreadIndex(numUnfinishedThreads);
            while(idx == runningThreadIndex)
            {
                idx = GetNextThreadIndex(numUnfinishedThreads);    
            }
            return idx;
        }

        public int GetNextThreadIndex(int numUnfinishedThreads) => _random.Next(numUnfinishedThreads);

        public bool NewIteration()
        {
            ++_iterationCount;
            return _iterationCount <= _numIterations;
        }

        public bool Finished => _iterationCount >= _numIterations;
    }
}