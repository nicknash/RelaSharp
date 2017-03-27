using System;
using System.Text;

namespace RelaSharp
{
    class VectorClock // I guess this should really be named VersionVector 
    {
        public const long MaxTime = long.MaxValue;
        private long[] _clocks;
        public readonly int Size;
        
        public VectorClock(int size)
        {
            if(size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }
            _clocks = new long[size];
            Size = size;
        }

        private void CheckSize(VectorClock other)
        {
            if(Size != other.Size)
            {
                throw new Exception($"Cannot compare vector clocks of different sizes, this size = {Size}, other size = {other.Size}");
            }
        }

        public bool AnyGreaterOrEqual(VectorClock other)
        {
            return Any(other, (x, y) => x >= y);
        }

        public bool AnyGreater(VectorClock other)
        {
            return Any(other, (x, y) => x > y);
        }

        private bool Any(VectorClock other, Func<long, long, bool> cmp)
        {
            CheckSize(other);            
            for(int i = 0; i < Size; ++i)
            {
                if(cmp(_clocks[i], other._clocks[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public long this[int idx]
        {
            get
            {
                return _clocks[idx];
            }
            set
            {
                _clocks[idx] = value;
            }

        }

        public void SetAllClocks(long v)
        {
            for(int i = 0; i < Size; ++i)
            {
                _clocks[i] = v;
            }
        }

        public void Join(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(other._clocks[i] > _clocks[i])
                {
                    _clocks[i] = other._clocks[i];
                }
            }
        }

        public void Assign(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                _clocks[i] = other._clocks[i];
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder(Size);
            sb.Append(_clocks[0]);
            for(int i = 1; i < Size; ++i)
            {
                sb.Append($"^{_clocks[i]}");
            }
            return sb.ToString();
        }
    }
}