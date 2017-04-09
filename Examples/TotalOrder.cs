using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
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
    
}