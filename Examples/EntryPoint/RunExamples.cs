﻿using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using RelaSharp.Examples;
using RelaSharp.Examples.CLR;
using RelaSharp.Scheduling;

namespace RelaSharp.EntryPoint
{
    public class RunExamples
    {
        public static void Main(string[] args)
        {
            var options = Options.GetOptions(args);
            if(args.Length == 0 || options.Help)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine(Options.GetHelp());
                return;
            }
            RelaEngine.Mode = EngineMode.Test;
            Func<string, IRelaExample, Tuple<string, IRelaExample>> c = (s, i) =>  Tuple.Create<string, IRelaExample>(s, i); 
            var examples = new Tuple<string, IRelaExample>[]{c("SimpleAcquireRelease", new SimpleAcquireRelease()), 
                                                             c("StoreLoad", new StoreLoad()), 
                                                             c("SPSC", new BoundedSPSCQueue()), 
                                                             c("Petersen", new Petersen()), 
                                                             c("TotalOrder", new TotalOrder()),
                                                             c("TransitiveLastSeen", new TransitiveLastSeen()), 
                                                             c("SimpleTransitive", new SimpleTransitive()),
                                                             c("RelaxedModOrder", new RelaxedModificationOrder()),
                                                             c("LiveLock", new LiveLock()),
                                                             c("Treiber", new TreiberStack()),
                                                             c("MichaelScott", new MichaelScottQueue()),
                                                             c("Deadlock", new Deadlock()),
                                                             c("LostWakeUp", new LostWakeUp()),
                                                             c("CorrectLeftRight", new LeftRight()),
                                                             c("BuggyLeftRight", new IncorrectLeftRight()),
                                                             c("StarvationLeftRight", new StarvationLeftRight()),
                                                             c("MultiWriterCOWList", new COWList()),
                                                             c("SimpleReadIndicator", new SingleCounterReadIndicator()),
                                                             c("DCLReadIndicator", new DCLReadIndicator()),
                                                             c("AsymmetricPetersen", new AsymmetricPetersen()),
                                                             c("AsymmetricLock", new AsymmetricLock()),
                                                             c("SafeAsymmetricLock", new SafeAsymmetricLock()),
                                                             c("MinimalIPI", new MinimalIPI()),
                                                             c("IPIReadWriteLock", new IPIReadWriteLock()) };
            if(options.ListExamples)
            {
                Console.WriteLine("Available examples:");
                Console.WriteLine("-------------------");
                Console.WriteLine(String.Join("\n", examples.Select(e => $"{e.Item1}\r\t\t\t{e.Item2.Name}")));
                return;
            }
            for(int i = 0; i < examples.Length; ++i)
            {
                var tag = examples[i].Item1;
                var example = examples[i].Item2;
                if(options.TestTag == null || Regex.Match(tag, options.TestTag, RegexOptions.IgnoreCase) != Match.Empty)
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
            var TR = TestRunner.TR;
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
                IScheduler scheduler;
                example.PrepareForIteration();
                int numThreads = example.ThreadEntries.Count;
                switch(options.Scheduling)
                {
                    case Options.SchedulingAlgorithm.Random:
                        scheduler = new NaiveRandomScheduler(numThreads, options.Iterations);
                    break;
                    case Options.SchedulingAlgorithm.Exhaustive:
                        scheduler = new ExhaustiveScheduler(numThreads, options.LiveLockLimit * 2, options.YieldLookbackPenalty);
                    break;
                    default:
                        throw new Exception($"Unsupported scheduling algorithm '{options.Scheduling}'");
                }
                while(scheduler.NewIteration() && !testFailed)
                {
                    example.PrepareForIteration();
                    TR.RunTest(example, scheduler, options.LiveLockLimit);
                    testFailed = TR.TestFailed;
                    totalOperations += TR.ExecutionLength;
                    ++numIterations;
                }
                var panic = "*\n*\n*\n*\n*\n*\n*\n*";
                if (TR.TestFailed)
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
                        TR.DumpExecutionLog(Console.Out);
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
                    Console.WriteLine($"Tested {totalOperations / elapsed:F3} operations per second ({numIterations} iterations at {(numIterations) / elapsed:F3} iterations per second) for {elapsed} seconds.");
                    Console.WriteLine("..........................");
                }
            }
        }
    }
}
