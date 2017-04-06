﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class RInterlocked<T> where T : IEquatable<T>
    {
        private MemoryOrdered<T> _data;

        private int y = 12345;


        private HashSet<long> seen = new HashSet<long>();
        private List<GCHandle> handles = new List<GCHandle>();
        public unsafe void f(ref int x)
        {
            fixed(int* p = &x)
            {
                long q = (long) p;
                if(seen.Add(q))
                {
                    Console.WriteLine(q);
                    GCHandle pin = GCHandle.Alloc(x, GCHandleType.Pinned);
                    GCHandle pin2 = GCHandle.Alloc(*p, GCHandleType.Pinned);
                    Console.WriteLine(pin.AddrOfPinnedObject());
                    handles.Add(pin);            
                }
                /*if(seen.Count > 1)
                {
                    Console.WriteLine("MOVED");
                }*/
                //Console.WriteLine(q);
            }
            /*
            GCHandle pin = GCHandle.Alloc(x, GCHandleType.Pinned);
            x = 98765;
            Console.WriteLine(pin.AddrOfPinnedObject());
            */
        }

        public void g()
        {
            for(int i = 0; i < 100000; ++i) 
            {
                int[] x = new int[1234];
                //GC.Collect();
                f(ref y);
            }
            Console.WriteLine(y);
        }
    }


    public class Program
    {
        public static void Main(string[] args)
        {
            int i;
            var sw = new Stopwatch();
            int numIterations = 10000;
            sw.Start();
            for(i = 0; i < numIterations; ++i)
            {
                //var test = new StoreLoad();
                //var test = new PetersenTest(MemoryOrder.AcquireRelease);
                //var test = new TotalOrderTest(MemoryOrder.AcquireRelease);
                var test = new BoundedSPSCQueueTest(MemoryOrder.Relaxed, 3);
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
            Console.WriteLine($"Tested {i / sw.Elapsed.TotalSeconds} executions per second");
        }
    }

    class BoundedSPSCQueueTest : ITest
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
                _data[w].Store(x, _memoryOrder);
                _write.Store((w + 1) % _size);
                return true;
            }

            public object Dequeue()
            {
                var r = _read.Load();
                var result = _data[r].Load(_memoryOrder);
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
                x.Store(i, MemoryOrder.Relaxed);
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
                    TE.Assert(z == numDequeued, $"Partially constructed object detected: value = {z}, numDequeued = {numDequeued}");
                    numDequeued++;
                }
            }
        }

        public void OnFinished()
        {

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

    class TotalOrderTest : ITest 
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
