using System;
using System.Text;

namespace RelaSharp
{
    class VectorClock
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
        public bool IsAfter(VectorClock other)
        {
            CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(_clocks[i] > other._clocks[i])
                {
                    // i.e., return true if there exists i such that _clocks[i] > other._clocks[i]                    
                    return true;
                }
            }
            // i.e., _clocks[i] <= other._clocks[i] for all i
            return false;            
        }

        public bool IsBefore(VectorClock other)
        {
           CheckSize(other);
            for(int i = 0; i < Size; ++i)
            {
                if(_clocks[i] > other._clocks[i])
                {
                    // i.e., return false if there exists i such that _clocks[i] > other._clocks[i]                    
                    return false;
                }
            }
            // i.e., _clocks[i] <= other._clocks[i] for all i
            return true;            
        }

        // TODO: Elaborate and possible rename "IsAfter" and "IsBefore"
        // Noteworthy examples of IsAfter and IsBefore
        // IsAfter(10, 01) == false 
        // IsBefore(10, 01) == false
        // i.e. IsAfter != !IsBefore

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