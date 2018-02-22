namespace RelaSharp.CLR
{
    public class CLRAtomicInt
    {
        private readonly IAtomicInt _atomic;
        private CLRAtomicInt()
        {
            switch(RelaEngine.Mode)
            {
                case EngineMode.Test:
                    _atomic = new AtomicInt();
                    break;
                case EngineMode.Live:
                    _atomic = new Live.LiveAtomicInt();
                    break;
                default:
                throw new EngineException($"{nameof(CLRAtomicInt)} must only be used when RelaEngine.Mode is {EngineMode.Test} or {EngineMode.Live}, but it is {RelaEngine.Mode} (did you forget to assign it?).");
            }
        }

        internal static IAtomicInt Get(ref CLRAtomicInt data)
        {
            if(data == null)
            {
                data = new CLRAtomicInt();
            }
            return data._atomic;
        }
    }
}