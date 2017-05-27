namespace RelaSharp.Scheduling.Exhaustive
{
    class PriorityRelation
    {
        private readonly int _numThreads;
        private readonly bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.
        private readonly ThreadSet _ready;
        private int[] _priorityOver;

        public PriorityRelation(int numThreads)
        {
            _numThreads = numThreads;
            _hasPriority = new bool[numThreads, numThreads];
            _ready = new ThreadSet(numThreads);
            _priorityOver = new int[numThreads];
            ComputeReady();
        }

        public void GivePriorityOver(int x, ThreadSet threads)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                if(threads.Contains(i))
                {
                    _hasPriority[x, i] = true;
                }
            }
            _priorityOver[x] += threads.NumElems;
            ComputeReady();
        }

        public void RemovePriorityOf(int x)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                _hasPriority[i, x] = false;
                if(_priorityOver[i] > 0)
                {
                    _priorityOver[i]--;
                }
            }
            ComputeReady();
        }

        private void ComputeReady()
        {
            _ready.Clear();
            for (int i = 0; i < _numThreads; ++i)
            {
                if (_priorityOver[i] == 0)
                {
                    _ready.Add(i);
                }
            }
        }

        public ThreadSet GetSchedulableThreads(ThreadSet enabled)
        {
            var result = new ThreadSet(_numThreads);
            result.ReplaceWith(enabled);
            result.IntersectWith(_ready);
            return result;
        }
    }
}