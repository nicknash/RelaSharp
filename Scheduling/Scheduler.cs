using System;
using System.Linq;

namespace RelaSharp.Scheduling
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
            if(idx != -1)
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
            if(idx == -1)
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

    class Scheduler
    {
        private ArraySet _unfinishedThreadIds;
        private ArraySet _waitingThreadIds;
        private ArraySet _threadIdsSeenWhileAllWaiting;
        private ISchedulingAlgorithm _schedulingAlgorithm;
        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;
        public int RunningThreadId { get; private set; }
        public bool Finished => AllThreadsFinished && _schedulingAlgorithm.Finished;

        public Scheduler(int numThreads, ISchedulingAlgorithm schedulingAlgorithm)
        {
            _schedulingAlgorithm = schedulingAlgorithm;
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
            var idx = _schedulingAlgorithm.GetNextThreadIndex(_unfinishedThreadIds.NumElems);
            RunningThreadId = _unfinishedThreadIds[idx]; 
            return;
        }

        public bool ThreadWaiting()
        {
            int numUnfinished = _unfinishedThreadIds.NumElems;
            _waitingThreadIds.Add(RunningThreadId);
            if(_waitingThreadIds.NumElems == numUnfinished)
            {
                _threadIdsSeenWhileAllWaiting.Add(RunningThreadId);
            }
            else
            {
                _threadIdsSeenWhileAllWaiting.Clear();
            }
            bool deadlock = _unfinishedThreadIds.NumElems == 1 ||  _threadIdsSeenWhileAllWaiting.NumElems == _unfinishedThreadIds.NumElems;
            int originalId = RunningThreadId;
            while(!deadlock && RunningThreadId == originalId) 
            {
                MaybeSwitch();
            }
            return deadlock;
        } 

        public void ThreadFinishedWaiting() => _waitingThreadIds.Remove(RunningThreadId);  // TODO: Should clear _threadIdsSeenWhileAllWaiting() ?
    
        public void ThreadFinished() 
        {
            _unfinishedThreadIds.Remove(RunningThreadId);
            //Console.WriteLine($"thread finished: {RunningThreadId}, num unfinished = {_unfinishedThreadIds.NumElems}");
            if(!AllThreadsFinished)
            {
                MaybeSwitch();
            }
        }
    }
}