using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RelaSharp.EntryPoint
{
    class Options
    {
        public enum SchedulingAlgorithm
        {
            Random,
            Exhaustive
        }
        public const int DefaultIterations = 10000;
        public const SchedulingAlgorithm DefaultScheduling = SchedulingAlgorithm.Random;
        public bool Help;
        public readonly bool QuietMode;
        public readonly bool SelfTest;
        public readonly string TestTag;
        public readonly int Iterations;
        public readonly bool ListExamples;
        public readonly SchedulingAlgorithm Scheduling;

        public Options(bool help, bool quietMode, bool selfTest, string testTag, int iterations, bool listExamples, SchedulingAlgorithm scheduling)
        {
            Help = help;
            QuietMode = quietMode;
            SelfTest = selfTest;
            TestTag = testTag;
            Iterations = iterations;
            ListExamples = listExamples;
            Scheduling = scheduling;
        }

        public static Options GetOptions(string[] args)
        {
            Func<string, int, string> takeTrim = (arg, idx) => arg.Contains('=') ? arg.Split('=')[idx].Trim() : arg.Trim();
            var argMap = args.ToDictionary(a => takeTrim(a, 0), a => takeTrim(a, 1));
            bool quietMode = argMap.ContainsKey("--quiet");
            int iterations = GetOptionValue("--iterations", argMap, Int32.Parse, DefaultIterations, Console.Error);
            bool selfTest = argMap.ContainsKey("--self-test");
            string testTag = GetOptionValue("--tag", argMap, s => s, null, Console.Error);
            
            bool listExamples = argMap.ContainsKey("--list-examples");
            bool help = argMap.ContainsKey("--help");
            SchedulingAlgorithm scheduling = GetOptionValue("--scheduling", argMap, s => (SchedulingAlgorithm) Enum.Parse(typeof(SchedulingAlgorithm), s, true), DefaultScheduling, Console.Error);
            return new Options(help, quietMode, selfTest, testTag, iterations, listExamples, scheduling);
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
                                                              {"--iterations=X", $"For random scheduler only: Run for X iterations (defaults to {DefaultIterations})"},
                                                              {"--self-test", "Run self test mode (suppress all output and only report results that differ from expected results)"},
                                                              {"--tag=X", "Run examples whose name contain the tag (case insensitive, run all examples if unspecified)"},
                                                              {"--scheduling=X", $"Use the specified scheduling algorithm, available options are 'random' and 'exhaustive' (defaults to {DefaultScheduling})"},
                                                              {"--list-examples", "List the tags of the available examples with their full names (and then exit)."},
                                                              {"--help", "Print this message and exit"}
                                                            };
            var result = String.Join(Environment.NewLine, allOptions.Select(kvp => $"{kvp.Key}\r\t\t{kvp.Value}"));
            return result;
        }
    }
}