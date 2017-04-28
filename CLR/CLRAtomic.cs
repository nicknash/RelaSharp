namespace RelaSharp.CLR
{
    class CLRAtomic<T>
    {
        public readonly Atomic<T> __internal;

        public CLRAtomic()
        {
            __internal = new Atomic<T>();
        }
    }    

    class CLRAtomic32
    {
        public readonly Atomic32 __internal;
        public CLRAtomic32()
        {
            __internal = new Atomic32();
        }
    }

    class CLRAtomic64
    {
        public readonly Atomic64 __internal;
        public CLRAtomic64()
        {
            __internal = new Atomic64();
        }
    }

}