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
                var test = new WhoIsFirst(MemoryOrder.AcquireRelease);
                //var test = new StoreLoad();
                //var test = new PetersenTest(MemoryOrder.AcquireRelease);
                TestEnvironment.TE.RunTest(test);         
                if(TestEnvironment.TE.TestFailed)
                {
                    break;
                }
            }
            if(TestEnvironment.TE.TestFailed)
            {
                Console.WriteLine($"Test failed on iteration: {i}");
                TestEnvironment.TE.DumpExecutionLog(Console.Out);
            }
            else
            {
                Console.WriteLine($"No failures after {i} iterations");
            }
        }
    }

    class PetersenTest : ITest 
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private MemoryOrdered<int> flag0;
        private MemoryOrdered<int> flag1;
        private MemoryOrdered<int> victim;
        private RaceChecked<int> _canary;
        private MemoryOrder _memoryOrder;
        public IReadOnlyList<Action> ThreadEntries { get; private set;}

        int _threadsPassed = 0;

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
            //_canary.Store(25);
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");            
            flag0.Store(0, _memoryOrder);
            --_threadsPassed;
        }

        private void Thread1()
        {
            flag1.Store(1, _memoryOrder);
            victim.Store(1, _memoryOrder);            
            while(flag0.Load(_memoryOrder) == 1 && victim.Load(_memoryOrder) == 1) ;        
            ++_threadsPassed;
            TE.Assert(_threadsPassed == 1, $"Mutual exclusion not achieved, {_threadsPassed} threads currently in critical section!");
            //_canary.Store(25);
            flag1.Store(0, _memoryOrder);
            --_threadsPassed;
        }

        public void OnFinished()
        {

        }
    }

    class WhoIsFirst : ITest 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }
        private static TestEnvironment TE = TestEnvironment.TE;

        private MemoryOrdered<int> x, y;

        private MemoryOrder _memoryOrder;

        private int y0, y1;
        private bool _thread1IsFirst;
        private bool _thread2IsFirst;

        public WhoIsFirst(MemoryOrder memoryOrder)
        {
            _memoryOrder = memoryOrder;
            ThreadEntries = new List<Action> {Thread1,Thread2};
            x = new MemoryOrdered<int>();
            y = new MemoryOrdered<int>();
        }

        public void Thread1()
        {
            x.Store(1, _memoryOrder);
            _thread1IsFirst = y.Load(_memoryOrder) == 0;
        }

        public void Thread2()
        {
             y.Store(1, _memoryOrder);
            _thread2IsFirst = x.Load(_memoryOrder) == 0;
       }

        public void OnFinished()
        {
            TE.Assert(!_thread1IsFirst || !_thread2IsFirst, "Both threads were first! (store load reordering!)");
        }
    }


    public class StoreLoad : ITest 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }
        private static TestEnvironment TE = TestEnvironment.TE;

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
            // Should throw an exception if any MemoryOrdered or RaceChecked are touched.
            TE.Assert(y0 != 0 || y1 != 0, "Both of y0 and y1 are zero! (store load reordering!)");
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
