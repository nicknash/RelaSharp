using System.Text;

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

        public int Successor(int idx)
        {
            int i = idx;
            while(i < _elems.Length && !_elems[i])
            {
                ++i;
            }
            return i; // Return _elems.Length if no successor
        }

        public bool this[int idx] => _elems[idx]; 

        public void Clear()
        {
            if (NumElems > 0)
            {
                NumElems = 0;
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

       public override string ToString()
        {
            var sb = new StringBuilder(_elems.Length);
            sb.Append(_elems[0] ? "1" : "0");
            for(int i = 1; i < _elems.Length; ++i)
            {
                var e = _elems[i] ? 1 : 0;
                sb.Append($"^{e}");
            }
            return sb.ToString();
        }
    }
}