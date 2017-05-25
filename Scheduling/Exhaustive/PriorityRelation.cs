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
        }

        // Give all threads priority over x
        public void GivePriorityOver(int x)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                _hasPriority[x, i] = true;
            }
            _priorityOver[x] += _numThreads;
            ComputeReady();
        }

        // Remove priority of x over all threads.
        public void RemovePriorityOf(int x)
        {
            for (int i = 0; i < _numThreads; ++i)
            {
                _hasPriority[i, x] = false;
                _priorityOver[i]--; // TODO: Check positive reqd?
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
            return enabled.Intersection(_ready);
        }
    }
}