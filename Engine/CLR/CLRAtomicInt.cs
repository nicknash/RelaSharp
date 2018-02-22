namespace RelaSharp.CLR
{
    public class CLRAtomicInt
    {
        public readonly AtomicInt _atomic;
        private CLRAtomicInt()
        {
            _atomic = new AtomicInt();
        }

        internal static AtomicInt Get(ref CLRAtomicInt data)
        {
            if(data == null)
            {
                data = new CLRAtomicInt();
            }
            return data._atomic;
        }
    }
}