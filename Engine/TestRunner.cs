using System;
using System.IO;
using RelaSharp.Scheduling;

namespace RelaSharp
{
    public class TestRunner // TODO: Should the RunTest() functions sanity check EngineMode?
    {
        public static TestRunner TR = new TestRunner();

        private static TestEnvironment TE = TestEnvironment.TE; 
        public void RunTest(IRelaTest test, IScheduler scheduler, ulong liveLockLimit) // Maybe re-name to RunTestOnce
         => TE.RunTest(test, scheduler, liveLockLimit);

        public void RunTest(Func<IRelaTest> makeTest, int numIterations = 10000, ulong liveLockLimit = 5000)
        {
            var test = makeTest();
            var scheduler = new NaiveRandomScheduler(test.ThreadEntries.Count, numIterations);
            while(scheduler.NewIteration() && !TestFailed)
            {
                TE.RunTest(test, scheduler, liveLockLimit);
                test = makeTest();
            }
        }

        public bool TestFailed => TE.TestFailed;

        public ulong ExecutionLength => TE.ExecutionLength;
    
        public void DumpExecutionLog(TextWriter output)
         => TE.DumpExecutionLog(output);
    }
}