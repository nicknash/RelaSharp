using System;

namespace RelaSharp.Scheduling.Exhaustive
{
    class Choice
    {
        public int Chosen { get; private set; }
        public readonly ThreadSet _alternatives;
        public bool FullyExplored => _numElemsSeen >= _alternatives.NumElems;
        private int _numElemsSeen;

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

        public void Next()
        {
            if (FullyExplored)
            {
                throw new Exception("Scheduling choice already exhausted.");
            }
            do
            {
                Chosen++;
            } while (!_alternatives[Chosen]);
            _numElemsSeen++;
        }
    }

}