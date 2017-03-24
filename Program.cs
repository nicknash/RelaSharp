using System;
using System.Threading;
using System.Collections.Generic;

namespace RelaSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
        
            var test = new PetersenTest();
            TestEnvironment.TE.RunTest(test);
            
            //TestRunner.Run(test);
            //TestEnvironment.TE.SetupTest();
            
        }
    }

    public class PetersenTest : ITest 
    {
        private MemoryOrdered<int> flag0;
        private MemoryOrdered<int> flag1;
        private MemoryOrdered<int> turn;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}

        public PetersenTest()
        {
            ThreadEntries = new List<Action>{Thread1,Thread2};
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
        }

        private void Thread1()
        {
            //while(true)
            for(int i = 0; i < 10; ++i)
            {
                //Console.WriteLine($"Storing {i}");
                //flag0.Load(i, MemoryOrder.Acquire);
                Console.WriteLine($"Loaded {flag0.Load(MemoryOrder.Acquire)}");
            }
        }

        private void Thread2()
        {
            //while(true)
            for(int i = 0; i < 5; ++i)
            {
                Console.WriteLine($"Storing {i}");
                flag0.Store(i, MemoryOrder.Release);
            }
        }

/*
        void Setup()
        {
        //     flag0 = 0;
        //     flag1 = 0;
        //     turn = 0;
        }*/
    }

}
