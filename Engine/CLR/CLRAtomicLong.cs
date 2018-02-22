namespace RelaSharp.CLR
{
    public class CLRAtomicLong
    {
        public readonly AtomicLong _atomic;
        private CLRAtomicLong()
        {
            _atomic = new AtomicLong();
        }
 
        internal static AtomicLong Get(ref CLRAtomicLong data)
        {
            if(data == null)
            {
                data = new CLRAtomicLong();
            }
            return data._atomic;
        }

    }
}