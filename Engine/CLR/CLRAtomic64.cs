namespace RelaSharp.CLR
{
    public class CLRAtomic64
    {
        public readonly Atomic64 _atomic;
        private CLRAtomic64()
        {
            _atomic = new Atomic64();
        }
 
        internal static Atomic64 Get(ref CLRAtomic64 data)
        {
            if(data == null)
            {
                data = new CLRAtomic64();
            }
            return data._atomic;
        }

    }
}