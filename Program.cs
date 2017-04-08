using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RelaSharp
{
    class RInterlocked<T> where T : IEquatable<T>
    {
        private MemoryOrdered<T> _data;

        private int y = 12345;
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            var example = new Examples.StoreLoad();
            while (example.SetNextConfiguration())
            {
                int i;
                var sw = new Stopwatch();
                int numIterations = 25000;
                sw.Start();
                for (i = 0; i < numIterations; ++i)
                {
                    //var test = new StoreLoad();
                    //var test = new PetersenTest(MemoryOrder.AcquireRelease);
                    //var test = new TotalOrderTest(MemoryOrder.AcquireRelease);
                    //var test = new BoundedSPSCQueueTest(MemoryOrder.Relaxed, 3);
                    TestEnvironment.TE.RunTest(example);
                    if (TestEnvironment.TE.TestFailed)
                    {
                        break;
                    }
                }
                if (TestEnvironment.TE.TestFailed)
                {
                    Console.WriteLine($"Example failed on iteration: {i}");
                    if(example.ExpectedToFail)
                    {
                        Console.WriteLine("Uh-oh: This example was not expected to fail.");
                    }
                    else
                    {
                        Console.WriteLine("Not to worry, this failure was expected");
                    }
                    TestEnvironment.TE.DumpExecutionLog(Console.Out);
                }
                else
                {
                    Console.WriteLine($"No failures after {i} iterations");
                }
                Console.WriteLine($"Tested {i / sw.Elapsed.TotalSeconds} executions per second");
            }
        }
    }
    class BoundedSPSCQueueTest : IRelaTest
    {
        class BoundedSPSCQueue 
        {
            private MemoryOrdered<object>[] _data;
            private RaceChecked<int> _read = new RaceChecked<int>();
            private RaceChecked<int> _write = new RaceChecked<int>();

            private int _size;

            private MemoryOrder _memoryOrder;
            public BoundedSPSCQueue(int size, MemoryOrder memoryOrder)
            {
                _data = new MemoryOrdered<object>[size];
                for(int i = 0; i < size; ++i)
                {
                    _data[i] = new MemoryOrdered<object>();
                }
                _size = size;
                _memoryOrder = memoryOrder;
            }

            public bool Enqueue(object x)
            {
                var w = _write.Load();
                if(_data[w].Load(_memoryOrder) != null)
                {
                    return false;
                }
                Fence.Insert(MemoryOrder.Release);
                _data[w].Store(x, _memoryOrder);
                _write.Store((w + 1) % _size);
                return true;
            }

            public object Dequeue()
            {
                var r = _read.Load();
                var result = _data[r].Load(_memoryOrder);
                Fence.Insert(MemoryOrder.Acquire);
                if(result == null)
                {
                    return null;
                }
                _data[r].Store(null, _memoryOrder);
                _read.Store((r + 1) % _size);
                return result;
            }
        
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set;}

        private BoundedSPSCQueue _queue;

        private MemoryOrder _memoryOrder;

        private int _size;

        public BoundedSPSCQueueTest(MemoryOrder memoryOrder, int size)
        {
            ThreadEntries = new List<Action>{Producer, Consumer};
            _queue = new BoundedSPSCQueue(size, memoryOrder);
            _memoryOrder = memoryOrder;
            _size = size;
        }
        private static TestEnvironment TE = TestEnvironment.TE;

        private void Producer()
        {
            for(int i = 0; i < _size; ++i)
            {
                var x = new MemoryOrdered<int>();
                x.Store(123 + i, MemoryOrder.Relaxed);
                _queue.Enqueue(x);
            }
        }

        private void Consumer()
        {
            int numDequeued = 0;
            while(numDequeued < _size)
            {
                var x = _queue.Dequeue();
                if(x != null)
                {
                    var y = (MemoryOrdered<int>) x;
                    var z = y.Load(MemoryOrder.Relaxed);
                    TE.Assert(z == 123 + numDequeued, $"Partially constructed object detected: value = {z}, numDequeued = {numDequeued}");
                    numDequeued++;
                }
            }
        }

        public void OnFinished()
        {

        }
    }


    class PetersenTest : IRelaTest 
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

    class TotalOrderTest : IRelaTest 
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private MemoryOrdered<int> a, b;
        private int c, d;
        private MemoryOrder _mo;

        public IReadOnlyList<Action> ThreadEntries { get; private set;}
    
        public TotalOrderTest(MemoryOrder mo)
        {
            _mo = mo;
            a = new MemoryOrdered<int>();
            b = new MemoryOrdered<int>();
            ThreadEntries = new List<Action> {Thread0, Thread1, Thread2, Thread3};
        }

        public void Thread0()
        {
            a.Store(1, _mo);
        }

        public void Thread1()
        {
            b.Store(1, _mo);
        } 

        public void Thread2()
        {
            if(a.Load(_mo) == 1 && b.Load(_mo) == 0)
            {
                c = 1;
            }
        }

        public void Thread3()
        {
            if(b.Load(_mo) == 1 && a.Load(_mo) == 0)
            {
                d = 1;
            }
        }

        public void OnFinished()
        {
            TE.Assert(c + d != 2, $"c + d == {c + d} ; neither of Thread0 or Thread1 ran first!");            
        }
    }

    public class AcqRelTest : IRelaTest
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
