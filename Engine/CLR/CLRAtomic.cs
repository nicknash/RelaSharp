namespace RelaSharp.CLR
{
    public class CLRAtomic<T> where T : class
    {
        private readonly IAtomic<T> _atomic;

        internal static IAtomic<T> GetAtomic()
        {
            switch(RelaEngine.Mode)
            {
                case EngineMode.Test:
                    return new Atomic<T>();
                case EngineMode.Live:
                    return new LiveAtomic<T>();
                default:
                throw new EngineException($"{nameof(CLRAtomic<T>)} must only be used when RelaEngine.Mode is {EngineMode.Test} or {EngineMode.Live}, but it is {RelaEngine.Mode} (did you forget to assign it?).");
            }
        }

        private CLRAtomic()
        {
            _atomic = GetAtomic();         
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