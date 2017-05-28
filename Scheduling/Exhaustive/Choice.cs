using System;

namespace RelaSharp.Scheduling.Exhaustive
{
    class Choice
    {
        public int Chosen { get; private set; }
        public readonly ThreadSet _alternatives;
        public bool FullyExplored => _numElemsSeen >= _alternatives.NumElems;
        private int _numElemsSeen;
        private int _maxLookback;
        private int _currentLookback;

        public Choice(ThreadSet alternatives)
        {
            _alternatives = alternatives;
            int n = alternatives.NumElems;
            if (n < 2)
            {
                throw new ArgumentOutOfRangeException($"Choice cannot be made between {n} or fewer alternatives");
            }
            while(!_alternatives[Chosen]) 
            {
                ++Chosen;
            } 
            _numElemsSeen = 1;
        }

        public int GetLookback(int maxLookback)
        {
            if(_maxLookback != 0 && maxLookback != _maxLookback)
            {
                throw new Exception($"Non-determinism detected: max look-back attempted to change from {_maxLookback} to {maxLookback}");
            }        
            _maxLookback = maxLookback;
            return _currentLookback;
        }

        public void Next()
        {
            if (FullyExplored)
            {
                throw new Exception("Scheduling choice already exhausted.");
            }
            if(_currentLookback < _maxLookback)
            {
                _currentLookback++;
                return;
            }
            _currentLookback = 0;
            _maxLookback = 0; // N.B., We chose a different thread, so may get a new maxLookback in GetLookback(..)
            do
            {
                Chosen++;
            } while (!_alternatives[Chosen]);
            _numElemsSeen++;
        }
    }
}