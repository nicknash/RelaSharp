using System;
using System.Diagnostics;
using RelaSharp.Examples;

namespace RelaSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var examples = new IRelaExample[]{new SimpleAcquireRelease(), new StoreLoad(), new BoundedSPSCQueue()};
            foreach(var example in examples)
            {
                Console.WriteLine($"Testing example: {example.Name}");
                RunExample(example);
            }
        }

        private static void RunExample(IRelaExample example)
        {
            while (example.SetNextConfiguration())
            {
                var expectedResult = example.ExpectedToFail ? "fail" : "pass";
                Console.WriteLine($"***** Current configuration for '{example.Name}' is '{example.Description}', this is expected to {expectedResult}");
                
                int i;
                var sw = new Stopwatch();
                int numIterations = 50000;
                sw.Start();
                for (i = 0; i < numIterations; ++i)
                {
                    //var test = new PetersenTest(MemoryOrder.AcquireRelease);
                    //var test = new TotalOrderTest(MemoryOrder.AcquireRelease);
                    //var test = new BoundedSPSCQueueTest(MemoryOrder.Relaxed, 3);
                    example.PrepareForIteration();
                    TestEnvironment.TE.RunTest(example);
                    if (TestEnvironment.TE.TestFailed)
                    {
                        break;
                    }
                }
                var panic = "*\n*\n*\n*\n*\n*\n*\n*";
                if (TestEnvironment.TE.TestFailed)
                {
                    Console.WriteLine($"Example failed on iteration: {i}");

                    Console.WriteLine(example.ExpectedToFail ?  "Not to worry, this failure was expected" : $"{panic}\tUh-oh: This example was expected to pass.\n{panic}");
                    TestEnvironment.TE.DumpExecutionLog(Console.Out);
                }
                else
                {
                    Console.WriteLine($"No failures after {i} iterations");
                    Console.WriteLine(example.ExpectedToFail ? $"{panic}\tUh-oh: This example was expected to fail.\n{panic}" : "That's good, this example was expected to pass.");
                }
                Console.WriteLine($"Tested {i / sw.Elapsed.TotalSeconds} executions per second");
                Console.WriteLine("..........................");
            }
        }
    }
}
