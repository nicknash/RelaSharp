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

        public int GetNextThreadIndex(int numUnfinishedThreads) => _random.Next(numUnfinishedThreads);

        public void NewIteration()
        {
            ++_iterationCount;
        }

        public bool Finished => _iterationCount >= _numIterations;
    }
}