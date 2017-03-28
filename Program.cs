using System;
using System.Collections.Generic;

namespace RelaSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int i;
            for(i = 0; i < 100; ++i)
            {
                //var test = new StoreLoad();
                var test = new PetersenTest(MemoryOrder.AcquireRelease);
                TestEnvironment.TE.RunTest(test);         
                if(test.Failed)
                {
            
                    break;
                }
            }
            if(TestEnvironment.TE.TestFailed)
            {
                Console.WriteLine($"Test failed on iteration: {i}");
                TestEnvironment.TE.DumpExecutionLog(Console.Out);
            }
        }
    }

    class PetersenTest : ITest 
    {
        private MemoryOrdered<int> flag0;
        private MemoryOrdered<int> flag1;
        private MemoryOrdered<int> victim;
        private RaceChecked<int> _canary;
        private MemoryOrder _memoryOrder;
        public IReadOnlyList<Action> ThreadEntries { get; private set;}

        int threadsPassed = 0;

        public PetersenTest(MemoryOrder memoryOrder)
        {
            _memoryOrder = memoryOrder;
            ThreadEntries = new List<Action>{Thread0,Thread1};
            flag0 = new MemoryOrdered<int>();
            flag1 = new MemoryOrdered<int>();
            victim = new MemoryOrdered<int>();
            _canary = new RaceChecked<int>();
        }

        private void Thread0()
        {
            flag0.Store(1, _memoryOrder);
            victim.Store(0, _memoryOrder);            
            while(flag1.Load(_memoryOrder) == 1 && victim.Load(_memoryOrder) == 0) ;        
            _canary.Store(25);
            ++threadsPassed;
            AssertMutualExclusion();
            flag0.Store(0, _memoryOrder);
            --threadsPassed;
        }

        private void Thread1()
        {
            flag1.Store(1, _memoryOrder);
            victim.Store(1, _memoryOrder);            
            while(flag0.Load(_memoryOrder) == 1 && victim.Load(_memoryOrder) == 1) ;        
            _canary.Store(25);
            ++threadsPassed;
            AssertMutualExclusion();
            flag1.Store(0, _memoryOrder);
            --threadsPassed;
        }

        private void AssertMutualExclusion()
        {
            if(threadsPassed > 1)
            {
                //Console.WriteLine("MUTEX FAILED!");
                Failed = true;
            }
        }
        public bool Failed;
        public void OnFinished()
        {

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


}
