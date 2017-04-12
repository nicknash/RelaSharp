using System;
using System.Diagnostics;
using RelaSharp.Examples;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RelaSharp
{
    public class Program
    {
        private class Options
        {
            public const int DefaultIterations = 10000;
            public bool Help;
            public readonly bool QuietMode;
            public readonly bool SelfTest;
            public readonly string TestTag;
            public readonly int Iterations;

            public Options(bool help, bool quietMode, bool selfTest, string testTag, int iterations)
            {
                Help = help;
                QuietMode = quietMode;
                SelfTest = selfTest;
                TestTag = testTag;
                Iterations = iterations;
            }

            public static Options GetOptions(string[] args)
            {
                Func<string, int, string> takeTrim = (arg, idx) => arg.Contains('=') ? arg.Split('=')[idx].Trim() : arg.Trim();
                var argMap = args.ToDictionary(a => takeTrim(a, 0), a => takeTrim(a, 1));
                const int defaultIterations = 10000;
                bool help = argMap.ContainsKey("--help");
                bool quietMode = argMap.ContainsKey("--quiet");
                bool selfTest = argMap.ContainsKey("--self-test");
                string testTag = GetOptionValue("--tag", argMap, s => s, null, Console.Error);
                int iterations = GetOptionValue("--iterations", argMap, Int32.Parse, defaultIterations, Console.Error);
                return new Options(help, quietMode, selfTest, testTag, iterations);
            }

            private static T GetOptionValue<T>(string option, Dictionary<string, string> argMap, Func<string, T> getValue, T defaultValue, TextWriter output)
            {
                string optionValue;
                if (!argMap.TryGetValue(option, out optionValue))
                {
                    return defaultValue;
                }
                try
                {
                    return getValue(optionValue);
                }
                catch (Exception)
                {
                    output.WriteLine($"Error parsing option {option} using {defaultValue} instead.");
                    return defaultValue;
                }
            }

            public static string GetHelp()
            {
                var allOptions = new Dictionary<string, string> { {"--quiet", "Suppress output of execution logs (defaults to false)"}, 
                                                                  {"--iterations=X", $"Run for X iterations (defaults to {DefaultIterations}"}, 
                                                                  {"--self-test", "Run self test mode (suppress all output and only report results that differ from expected results)"},
                                                                  {"--tag", "Run examples whose name contain the tag (case insensitive, run all examples if unspecified)"},
                                                                  {"--help", "Print this message and exit"}
                                                                };
                var result = String.Join(Environment.NewLine, allOptions.Select(kvp => $"{kvp.Key}\r\t\t{kvp.Value}"));
                return result;
            }
        }

        private class CASTest : IRelaTest
        {
            public IReadOnlyList<Action> ThreadEntries => _threads;
       
            private List<Action> _threads;
            public CASTest()
            {
               _threads = new List<Action>{Thread};
            }

            public void Thread()
            {
                var q = new MemoryOrdered<int>();
                q.Store(123, MemoryOrder.SequentiallyConsistent);
                Console.WriteLine(q.CompareExchange(1234, 123, MemoryOrder.AcquireRelease));
                TestEnvironment.TE.Assert(false, "deliberate failure");
            }

            public void OnFinished()
            {

            }
        }

        public static void Main(string[] args)
        {
            TestEnvironment.TE.RunTest(new CASTest());
                        TestEnvironment.TE.DumpExecutionLog(Console.Out);

            return;
            var options = Options.GetOptions(args);
            if(args.Length == 0 || options.Help)
            {
                Console.WriteLine(Options.GetHelp());
                return;
            }
            Func<string, IRelaExample, Tuple<string, IRelaExample>> c = (s, i) =>  Tuple.Create<string, IRelaExample>(s, i); 
            var examples = new Tuple<string, IRelaExample>[]{c("SimpleAcquireRelease", new SimpleAcquireRelease()), c("StoreLoad", new StoreLoad()), c("SPSC", new BoundedSPSCQueue()), 
                                                             c("Petersen", new Petersen()), c("TotalOrder", new TotalOrder())};
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
            while (example.SetNextConfiguration())
            {
                if(!options.SelfTest)
                {
                    var expectedResult = example.ExpectedToFail ? "fail" : "pass";
                    Console.WriteLine($"***** Current configuration for '{example.Name}' is '{example.Description}', this is expected to {expectedResult}");
                }
                int i;
                var sw = new Stopwatch();
                sw.Start();
                for (i = 0; i < options.Iterations; ++i)
                {
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
                    if(options.SelfTest)
                    {
                        if(!example.ExpectedToFail) 
                        {
                            Console.WriteLine($"{exampleTag}['{example.Description}'] expected to pass but failed on {i + 1}th iteration.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Example failed on iteration: {i}");
                        Console.WriteLine(example.ExpectedToFail ?  "Not to worry, this failure was expected" : $"{panic}\tUh-oh: This example was expected to pass.\n{panic}");
                    }
                    if(!options.QuietMode && !options.SelfTest)
                    {
                        TestEnvironment.TE.DumpExecutionLog(Console.Out);
                    }
                }
                else
                {
                    if(options.SelfTest)
                    {
                        if(example.ExpectedToFail)
                        {
                            Console.WriteLine($"{exampleTag}['{example.Description}'] expected to fail but survived {i} iterations.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No failures after {i} iterations");
                        Console.WriteLine(example.ExpectedToFail ? $"{panic}\tUh-oh: This example was expected to fail.\n{panic}" : "That's good, this example was expected to pass.");
                    }
                }
                if(!options.SelfTest)
                {
                    Console.WriteLine($"Tested {(i + 1) / sw.Elapsed.TotalSeconds} iterations per second.");
                    Console.WriteLine("..........................");
                }
            }
        }
    }
}
