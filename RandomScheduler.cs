using System;

namespace RelaSharp
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
    }

    class RandomScheduler
    {
        private Random _random = new Random();
        private ArraySet _unfinishedThreadIds;
        private ArraySet _waitingThreadIds;
        private ArraySet _threadIdsSeenWhileAllWaiting;

        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;

        public int RunningThreadId { get; private set; }

        public RandomScheduler(int numThreads)
        {
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
            var idx = _random.Next(_unfinishedThreadIds.NumElems);
            RunningThreadId = _unfinishedThreadIds[idx]; 
            return;
        }

        public bool ThreadWaiting()
        {
            _waitingThreadIds.Add(RunningThreadId);
            if(_waitingThreadIds.NumElems == _unfinishedThreadIds.NumElems)
            {
                _threadIdsSeenWhileAllWaiting.Add(RunningThreadId);
            }
            else
            {
                _threadIdsSeenWhileAllWaiting.Clear();
            }
            bool deadlock = _threadIdsSeenWhileAllWaiting.NumElems == _unfinishedThreadIds.NumElems;
            int originalId = RunningThreadId;
            while(RunningThreadId == originalId) 
            {
                MaybeSwitch();
            }
            return deadlock;
        } 

        public void ThreadFinishedWaiting() => _waitingThreadIds.Remove(RunningThreadId);  
    
        public void ThreadFinished() => _unfinishedThreadIds.Remove(RunningThreadId);
    }
}