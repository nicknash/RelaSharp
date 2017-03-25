using System;
using System.Collections.Generic;

namespace RelaSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
        
            //var test = new PetersenTest();
            //TestEnvironment.TE.RunTest(test);
            
            //var test2 = new PetersenTest();
            //TestEnvironment.TE.RunTest(test2);

            for(int i = 0; i < 25; ++i)
            {
                //Console.WriteLine($"{i} ***************");
                var test = new StoreLoad();
                TestEnvironment.TE.RunTest(test);         
            }
        }
    }

    public class StoreLoad : ITest 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        private MemoryOrdered<int> x0, x1;

        private int y0, y1;

        public StoreLoad()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            x0 = new MemoryOrdered<int>();
            x1 = new MemoryOrdered<int>();
        }

        public void Thread1()
        {
            x0.Store(1, MemoryOrder.Release);
            y0 = x1.Load(MemoryOrder.Acquire);
        }

        public void Thread2()
        {
            x1.Store(1, MemoryOrder.Release);
            y1 = x0.Load(MemoryOrder.Acquire);
        }

        public void OnFinished()
        {
            // This is kinda weird, what's the calling thread??
            if(y0 == 0 && y1 == 0)
            {
                Console.WriteLine("Both zero!");
            }
            else
            {
                Console.WriteLine($"{y0},{y1}");
            }
        }
    }

    public class AcqRelTest : ITest
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set;}
        
        private MemoryOrdered<int> flag = new MemoryOrdered<int>();

        private MemoryOrdered <int> x = new MemoryOrdered<int>();

        public AcqRelTest()
        {
            ThreadEntries = new List<Action> { Thread1, Thread2};
        }
        public void Thread1()
        {
            x.Store(23, MemoryOrder.Relaxed);
            x.Store(22, MemoryOrder.Relaxed);
            x.Store(21, MemoryOrder.Relaxed);
            x.Store(2, MemoryOrder.Relaxed);
            flag.Store(0, MemoryOrder.Relaxed);
            flag.Store(1, MemoryOrder.Release);
        }

        public void Thread2()
        {
            while(flag.Load(MemoryOrder.Acquire) == 0) 
            {            
            }
            int z = x.Load(MemoryOrder.Relaxed);
            if(z != 2)
            {
                Console.WriteLine($"WHOOOPS! {z}");
            }

        }

        public void OnFinished()
        {

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

        public void OnFinished()
        {

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
