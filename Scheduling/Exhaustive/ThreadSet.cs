namespace RelaSharp.Scheduling.Exhaustive
{
    class ThreadSet
    {
        private readonly bool[] _elems;
        public int NumElems { get; private set; }
        public int NumThreads { get; private set;}
        public ThreadSet(int numThreads)
        {
            NumThreads = numThreads;
            _elems = new bool[numThreads];
            NumElems = 0;
        }

        public void Add(int idx)
        {
            if(!_elems[idx])
            {
                _elems[idx] = true;
                ++NumElems;
            }
        }

        public void Remove(int idx)
        {
            if(_elems[idx])
            {
                _elems[idx] = false;
                --NumElems;
            }
        }

        public bool this[int idx] => _elems[idx]; 

        public void Clear()
        {
            if (NumElems > 0)
            {
                for (int i = 0; i < _elems.Length; ++i)
                {
                    _elems[i] = false;
                }
            }
        }

        public void ReplaceWith(ThreadSet other)
        {
            for (int i = 0; i < _elems.Length; ++i)
            {
                _elems[i] = other._elems[i];
            }
            NumElems = other.NumElems;
        }

        public bool Contains(int idx)
        {
            return _elems[idx];
        }

        public void IntersectWith(ThreadSet other)
        {
            NumElems = 0;
            for(int i = 0; i < _elems.Length; ++i)
            {
                _elems[i] &= other._elems[i];
                if(_elems[i])
                {
                    ++NumElems;
                }
            }
            return; 
        }

        public void LessWith(ThreadSet other)
        {
            NumElems = 0;
            for(int i = 0; i < _elems.Length; ++i)
            {
                _elems[i] &= !other._elems[i];
                if(_elems[i])
                {
                    ++NumElems;
                }
            }
        }

        public void UnionWith(ThreadSet other)
        {
            NumElems = 0;
            for(int i = 0; i < _elems.Length; ++i)
            {
                _elems[i] |= other._elems[i];
                if(_elems[i])
                {
                    ++NumElems;
                }
            }
        }
    }
}