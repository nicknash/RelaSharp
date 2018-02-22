namespace RelaSharp.CLR
{
    public class CLRAtomic32
    {
        public readonly Atomic32 _atomic;
        private CLRAtomic32()
        {
            _atomic = new Atomic32();
        }

        internal static Atomic32 Get(ref CLRAtomic32 data)
        {
            if(data == null)
            {
                data = new CLRAtomic32();
            }
            return data._atomic;
        }
    }
}