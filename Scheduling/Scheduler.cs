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
        private readonly ArraySet _unfinishedThreadIds;
        private readonly ArraySet _waitingThreadIds;
        private readonly ArraySet _threadIdsSeenWhileAllWaiting;
        private readonly ISchedulingAlgorithm _schedulingAlgorithm;
        private int _runningThreadIndex;
        private int NumUnfinishedThreads =>_unfinishedThreadIds.NumElems;
        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;
        public int RunningThreadId => _unfinishedThreadIds[_runningThreadIndex];

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
            _runningThreadIndex = _schedulingAlgorithm.GetNextThreadIndex(NumUnfinishedThreads);
            Console.WriteLine($"switch running thread = {RunningThreadId}");
            
            return;
        }

        public bool ThreadWaiting()
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
                _runningThreadIndex = _schedulingAlgorithm.WaitingGetNextThreadIndex(_runningThreadIndex, NumUnfinishedThreads);
            }
            Console.WriteLine($"running thread = {RunningThreadId}");
            return deadlock;
        } 

        public void ThreadFinishedWaiting() => _waitingThreadIds.Remove(RunningThreadId);  // TODO: Should clear _threadIdsSeenWhileAllWaiting() ?
    
        public void Yield()
        {
            _schedulingAlgorithm.Yield();
        }

        public void ThreadFinished() 
        {
            _unfinishedThreadIds.Remove(RunningThreadId);
            if(!AllThreadsFinished)
            {
                MaybeSwitch();
            }
        }
    }
}