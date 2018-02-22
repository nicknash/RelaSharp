namespace RelaSharp.CLR
{
    public class CLRAtomicLong
    {
        private readonly IAtomicLong _atomic;
        
        private CLRAtomicLong()
        {
            switch(RelaEngine.Mode)
            {
                case EngineMode.Test:
                    _atomic = new AtomicLong();
                    break;
                case EngineMode.Live:
                    _atomic = new Live.LiveAtomicLong();
                    break;
                default:
                throw new EngineException($"{nameof(CLRAtomicInt)} must only be used when RelaEngine.Mode is {EngineMode.Test} or {EngineMode.Live}, but it is {RelaEngine.Mode} (did you forget to assign it?).");
            }
            }
 
        internal static IAtomicLong Get(ref CLRAtomicLong data)
        {
            if(data == null)
            {
                data = new CLRAtomicLong();
            }
            return data._atomic;
        }

    }
}