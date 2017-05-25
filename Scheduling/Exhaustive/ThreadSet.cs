namespace RelaSharp.Scheduling.Exhaustive
{
    class ThreadSet
    {
        private readonly bool[] _elems;
        public int NumElems { get; private set; }
        public ThreadSet(int numThreads)
        {
            _elems = new bool[numThreads];
            NumElems = 0;
        }

        public void Add(int idx)
        {
            NumElems++;
            _elems[idx] = true;
        }

        public void Remove(int idx)
        {
            NumElems--;
            _elems[idx] = false;
        }

        public bool this[int idx] => _elems[idx];

        public void Clear()
        {
            if (NumElems > 0)
            {
                for (int i = 0; i < NumElems; ++i)
                {
                    _elems[i] = false;
                }
            }
        }

        public void ReplaceWith(ThreadSet other)
        {
            for (int i = 0; i < NumElems; ++i)
            {
                _elems[i] = other._elems[i];
            }
            NumElems = other.NumElems;
        }

        public bool Contains(int idx)
        {
            return _elems[idx];
        }

        public ThreadSet Intersection(ThreadSet other)
        {
            return null; // TODO: This is a garbage promoting interface, do differently?
        }

        public void LessWith(ThreadSet other)
        {

        }

        public void UnionWith(ThreadSet other)
        {

        }
    }

}