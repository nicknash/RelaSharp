using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
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

}