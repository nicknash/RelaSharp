using System;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    public enum EngineMode 
    {
        Undefined, // Use this in RunTest to blow up?
        Test,
        Live        
    }

    public interface IRelaEngine 
    {
        void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0);
        void Yield();
        void MaybeSwitch();
    }

    class TestRelaEngine : IRelaEngine
    {
        private static TestEnvironment TE = TestEnvironment.TE; 
        public void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
         => TE.Assert(shouldBeTrue, reason, memberName, sourceFilePath, sourceLineNumber);
        public void Yield() 
         => TE.Yield();
        public void MaybeSwitch()
         => TE.MaybeSwitch();
    }

    public class RelaEngine
    {
        public static EngineMode Mode;
        private static TestRelaEngine TestEngine = new TestRelaEngine();
        private static LiveRelaEngine LiveEngine = new LiveRelaEngine();
        public static IRelaEngine RE
        {
            get
            {
                switch (Mode)
                {
                    case EngineMode.Test:
                        return TestEngine;
                    case EngineMode.Live:
                        return LiveEngine;
                }
                throw new ArgumentOutOfRangeException($"Please set the {nameof(Mode)} property to either {EngineMode.Test} or {EngineMode.Live}. The latter is uninstrumented, for 'production' use.");
            }
        }
    }
}