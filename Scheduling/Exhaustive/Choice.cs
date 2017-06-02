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
            if (n == 0)
            {
                throw new ArgumentOutOfRangeException($"No alternatives to choose between.");
            }
            Chosen = _alternatives.Successor(0);
            _numElemsSeen = 1;
        }

        public void Next()
        {
            if (FullyExplored)
            {
                throw new Exception("Scheduling choice already exhausted.");
            }
            Chosen = _alternatives.Successor(Chosen + 1);
            _numElemsSeen++;
        }
    }
}