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
        private ArraySet _unfinishedThreadIds;
        private ArraySet _waitingThreadIds;
        private ArraySet _threadIdsSeenWhileAllWaiting;
        private int _runningThreadIndex;
        private int _numThreads;
        private int NumUnfinishedThreads => _unfinishedThreadIds.NumElems;
        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;
        public int RunningThreadId => _unfinishedThreadIds[_runningThreadIndex];
        private int _iterationCount;

        public NaiveRandomScheduler(int numThreads, int numIterations)
        {
            _numThreads = numThreads;
            _numIterations = numIterations;
            PrepareForScheduling();
            MaybeSwitch();
        }

        private void PrepareForScheduling()
        {
            _unfinishedThreadIds = new ArraySet(_numThreads);
            _waitingThreadIds = new ArraySet(_numThreads);
            _threadIdsSeenWhileAllWaiting = new ArraySet(_numThreads);
            for(int i = 0; i < _numThreads; ++i)
            {
                _unfinishedThreadIds.Add(i);
            }
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
                while(RunningThreadId == originalId)
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
            PrepareForScheduling();
            return _iterationCount <= _numIterations;
        }
    }
}