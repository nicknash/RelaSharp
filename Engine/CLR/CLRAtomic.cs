using RelaSharp.CLR.Live;

namespace RelaSharp.CLR
{
    public class CLRAtomic<T> where T : class
    {
        private readonly IAtomic<T> _atomic;

        private CLRAtomic()
        {
            switch(RelaEngine.Mode)
            {
                case EngineMode.Test:
                    _atomic = new Atomic<T>();
                    break;
                case EngineMode.Live:
                    _atomic = new LiveAtomic<T>();
                    break;
                default:
                throw new EngineException($"{nameof(CLRAtomic<T>)} must only be used when RelaEngine.Mode is {EngineMode.Test} or {EngineMode.Live}, but it is {RelaEngine.Mode} (did you forget to assign it?).");
            }
        }

        internal static IAtomic<T> Get(ref CLRAtomic<T> data)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            return data._atomic;
        }
    }    
}