using System;
using System.Diagnostics;
using RelaSharp.Examples;

namespace RelaSharp.EntryPoint
{
    public class RunExamples
    {
        public static void Main(string[] args)
        {
            var options = Options.GetOptions(args);
            if(args.Length == 0 || options.Help)
            {
                Console.WriteLine("Usage: ");
                Console.WriteLine(Options.GetHelp());
                return;
            }
            Func<string, IRelaExample, Tuple<string, IRelaExample>> c = (s, i) =>  Tuple.Create<string, IRelaExample>(s, i); 
            var examples = new Tuple<string, IRelaExample>[]{c("SimpleAcquireRelease", new SimpleAcquireRelease()), 
                                                             c("StoreLoad", new StoreLoad()), 
                                                             c("SPSC", new BoundedSPSCQueue()), 
                                                             c("Petersen", new Petersen()), 
                                                             c("TotalOrder", new TotalOrder()), 
                                                             c("LiveLock", new LiveLock()),
                                                             c("Treiber", new TreiberStack()),
                                                             c("MichaelScott", new MichaelScottQueue()),
                                                             c("Deadlock", new Deadlock()),
                                                             c("LostWakeUp", new LostWakeUp())};
            for(int i = 0; i < examples.Length; ++i)
            {
                var tag = examples[i].Item1;
                var example = examples[i].Item2;
                if(options.TestTag == null || tag.ToLower().Contains(options.TestTag.ToLower()))
                {
                    if(!options.SelfTest)
                    {
                        Console.WriteLine($"Running example [{i + 1}/{examples.Length}]: {tag}: {example.Name}");
                    }
                    RunExample(tag, example, options);
                }
            }
        }

        private static void RunExample(string exampleTag, IRelaExample example, Options options)
        {
            var TE = TestEnvironment.TE;
            while (example.SetNextConfiguration())
            {
                if(!options.SelfTest)
                {
                    var expectedResult = example.ExpectedToFail ? "fail" : "pass";
                    Console.WriteLine($"***** Current configuration for '{example.Name}' is '{example.Description}', this is expected to {expectedResult}");
                }
                var sw = new Stopwatch();
                sw.Start();
                int numIterations = 0;
                ulong totalOperations = 0;
                bool testFailed = false;
                while(numIterations < options.Iterations && !testFailed)
                {
                    example.PrepareForIteration();
                    TE.RunTest(example);
                    testFailed = TE.TestFailed;
                    totalOperations += TE.ExecutionLength;
                    ++numIterations;
                }
                var panic = "*\n*\n*\n*\n*\n*\n*\n*";
                if (TE.TestFailed)
                {
                    if(options.SelfTest)
                    {
                        if(!example.ExpectedToFail) 
                        {
                            Console.WriteLine($"{exampleTag}['{example.Description}'] expected to pass but failed on iteration number {numIterations}.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Example failed on iteration number: {numIterations}");
                        Console.WriteLine(example.ExpectedToFail ?  "Not to worry, this failure was expected" : $"{panic}\tUh-oh: This example was expected to pass.\n{panic}");
                    }
                    if(!options.QuietMode && !options.SelfTest)
                    {
                        TE.DumpExecutionLog(Console.Out);
                    }
                }
                else
                {
                    if(options.SelfTest)
                    {
                        if(example.ExpectedToFail)
                        {
                            Console.WriteLine($"{exampleTag}['{example.Description}'] expected to fail but survived {numIterations} iterations.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No failures after {numIterations} iterations");
                        Console.WriteLine(example.ExpectedToFail ? $"{panic}\tUh-oh: This example was expected to fail.\n{panic}" : "That's good, this example was expected to pass.");
                    }
                }
                if(!options.SelfTest)
                {
                    var elapsed = sw.Elapsed.TotalSeconds;
                    Console.WriteLine($"Tested {totalOperations / elapsed} operations per second ({numIterations} iterations at {(numIterations) / elapsed} iterations per second) for {elapsed} seconds.");
                    Console.WriteLine("..........................");
                }
            }
        }
    }
}
