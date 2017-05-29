namespace RelaSharp.MemoryModel
{ 
    class AccessDataPool<T>
    {
        public int CurrentIndex { get; private set; }
        public int SizeOccupied { get; private set; }
        private AccessData<T>[] _pool;

        public AccessDataPool(int length, int numThreads)
        {
            _pool = new AccessData<T>[length];
            for(int i = 0; i < length; ++i)
            {
                _pool[i] = new AccessData<T>(numThreads);
            }
        }

        public AccessData<T> GetNext()
        {
            CurrentIndex++;
            if(SizeOccupied < _pool.Length)
            {
                SizeOccupied = CurrentIndex;
            }
            return this[CurrentIndex];
        }

        public AccessData<T> this[int idx]
        {
            get
            {
                int wrapped = idx % _pool.Length;
                if(wrapped < 0)
                {
                    wrapped += _pool.Length;
                }
                return _pool[wrapped];
            }
        }
    } 
}
 