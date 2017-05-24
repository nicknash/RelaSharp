using System;
using System.Linq;

namespace RelaSharp.Scheduling
{
    class NaiveRandomScheduler : IScheduler
    {
        class ArraySet
        {
            private int[] _elems;
            public int NumElems { get; private set; }
            public ArraySet(int capacity)
            {
                _elems = new int[capacity];
            }

            public bool Add(int x)
            {
                int idx = Array.IndexOf(_elems, x, 0, NumElems);
                if (idx != -1)
                {
                    return false;
                }
                _elems[NumElems] = x;
                NumElems++;
                return true;
            }

            public bool Remove(int x)
            {
                int idx = Array.IndexOf(_elems, x, 0, NumElems);
                if (idx == -1)
                {
                    return false;
                }
                NumElems--;
                _elems[idx] = _elems[NumElems];
                return true;
            }

            public int this[int idx] => _elems[idx];

            public void Clear()
            {
                NumElems = 0;
            }

            public override string ToString() => $"{NumElems}:{String.Join(",", _elems.Take(NumElems))}";
        }

        private readonly Random _random = new Random();
        private readonly int _numIterations;
        private readonly ArraySet _unfinishedThreadIds;
        private readonly ArraySet _waitingThreadIds;
        private readonly ArraySet _threadIdsSeenWhileAllWaiting;
        private int _runningThreadIndex;
        private int NumUnfinishedThreads =>_unfinishedThreadIds.NumElems;
        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;
        public int RunningThreadId => _unfinishedThreadIds[_runningThreadIndex];
        private int _iterationCount;

        public NaiveRandomScheduler(int numThreads, int numIterations)
        {
            _numIterations = numIterations;
            _unfinishedThreadIds = new ArraySet(numThreads);
            _waitingThreadIds = new ArraySet(numThreads);
            _threadIdsSeenWhileAllWaiting = new ArraySet(numThreads);
            for(int i = 0; i < numThreads; ++i)
            {
                _unfinishedThreadIds.Add(i);
            }
            MaybeSwitch();
        }

        public void MaybeSwitch()
        {
            if(AllThreadsFinished)
            {
                throw new Exception("All threads finished. Who called?");
            }
            _runningThreadIndex = _random.Next(NumUnfinishedThreads);
            return;
        }

        public bool ThreadWaiting(int waitingOnThreadId, object lockObject)
        {
            _waitingThreadIds.Add(RunningThreadId);
            if(_waitingThreadIds.NumElems == NumUnfinishedThreads)
            {
                _threadIdsSeenWhileAllWaiting.Add(RunningThreadId);
            }
            else
            {
                _threadIdsSeenWhileAllWaiting.Clear();
            }
            bool deadlock = NumUnfinishedThreads == 1 ||  _threadIdsSeenWhileAllWaiting.NumElems == NumUnfinishedThreads;
            int originalId = RunningThreadId;
            if(!deadlock) 
            {
                while(_runningThreadIndex == originalId)
                {
                    MaybeSwitch();
                }
            }
            return deadlock;
        } 

        public void ThreadFinishedWaiting() => _waitingThreadIds.Remove(RunningThreadId);  // TODO: Should clear _threadIdsSeenWhileAllWaiting() ?
    
        public void Yield()
        {
        }

        public void LockReleased(object lockObject)
        {
        }

        public void ThreadFinished() 
        {
            _unfinishedThreadIds.Remove(RunningThreadId);
            if(!AllThreadsFinished)
            {
                MaybeSwitch();
            }
        }  

        public bool NewIteration()
        {
            ++_iterationCount;
            return _iterationCount <= _numIterations;
        }
    }
}