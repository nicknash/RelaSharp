namespace RelaSharp.Scheduling.Exhaustive
{
    class PriorityRelation
    {
        private readonly int _numThreads;
        private readonly bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.

        public PriorityRelation(int numThreads)
        {
            _numThreads = numThreads;
            _hasPriority = new bool[numThreads, numThreads];
        }

        public void GivePriorityOver(int x, ThreadSet threads)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                if(threads[i] && i != x)
                {
                    _hasPriority[x, i] = true;
                }
            }
        }

        public void RemovePriorityOf(int x)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                _hasPriority[i, x] = false;
            }
        }

        public ThreadSet GetSchedulableThreads(ThreadSet enabled)
        {
            var admissable = new ThreadSet(_numThreads);
            admissable.ReplaceWith(enabled);
            for(int i = 0; i < _numThreads; ++i)
            {
                if(!admissable[i])
                {
                    continue;
                }
                for(int j = 0; j < _numThreads; ++j)
                {
                    // Thread i cannot be scheduled if an enabled thread
                    // has priority over it.
                    if(_hasPriority[i, j] && enabled[j])
                    {
                        admissable.Remove(i);
                        break;
                    }
                }
            }
            return admissable;
        }
    }
}