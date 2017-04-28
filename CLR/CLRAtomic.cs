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
    static class CLRAtomicFactory
    {
        public static Atomic<T> GetAtomic<T>(ref CLRAtomic<T> data)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            return data.__internal;
        }

        public static Atomic32 GetAtomic32(ref CLRAtomic32 data)
        {
            if(data == null)
            {
                data = new CLRAtomic32();
            }
            return data.__internal;
        }
  
        public static Atomic64 GetAtomic64(ref CLRAtomic64 data)
        {
            if(data == null)
            {
                data = new CLRAtomic64();
            }
            return data.__internal;
        }
    }

}