namespace RelaSharp.CLR
{
    class CLRAtomic<T>
    {
        private readonly Atomic<T> _atomic;

        private CLRAtomic()
        {
            _atomic = new Atomic<T>();
        }

        public static Atomic<T> Get(ref CLRAtomic<T> data)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            return data._atomic;
        }
    }    

    class CLRAtomic32
    {
        public readonly Atomic32 _atomic;
        private CLRAtomic32()
        {
            _atomic = new Atomic32();
        }

        public static Atomic32 Get(ref CLRAtomic32 data)
        {
            if(data == null)
            {
                data = new CLRAtomic32();
            }
            return data._atomic;
        }
    }

    class CLRAtomic64
    {
        public readonly Atomic64 _atomic;
        private CLRAtomic64()
        {
            _atomic = new Atomic64();
        }
 
        public static Atomic64 Get(ref CLRAtomic64 data)
        {
            if(data == null)
            {
                data = new CLRAtomic64();
            }
            return data._atomic;
        }

    }
}