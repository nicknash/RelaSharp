using System;

namespace RelaSharp.Scheduling
{ 
    class ExhaustiveScheduler : IScheduler
    {
        class PriorityRelation
        {
            private readonly int _numThreads;
            private readonly bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.
            private readonly ThreadSet _ready;
            private int[] _priorityOver;

            public PriorityRelation(int numThreads)
            {
                _numThreads = numThreads;
                _hasPriority = new bool[numThreads, numThreads];
                _ready = new ThreadSet(numThreads);
            }

            // Give all threads priority over x
            public void GivePriorityOver(int x)
            {
                for(int i = 0; i < _numThreads; ++i)
                {
                    _hasPriority[x, i] = true;
                }
                _priorityOver[x] += _numThreads;
                ComputeReady();
            }

            // Remove priority of x over all threads.
            public void RemovePriorityOf(int x)
            {
                for(int i = 0; i < _numThreads; ++i)
                {
                    _hasPriority[i, x] = false;
                    _priorityOver[i]--; // TODO: Check positive reqd?
                }
                ComputeReady();
            }

            private void ComputeReady()
            {
                _ready.Clear();
                for(int i = 0; i < _numThreads; ++i)
                {
                    if(_priorityOver[i] == 0)
                    {
                        _ready.Add(i);
                    }
                }
            }

            public ThreadSet GetSchedulableThreads(ThreadSet enabled)
            {
                return enabled.Intersection(_ready);
            }
        }

        class SchedulingHistory // SchedulingStrategy?
        {
            private Choice[] _choices;
            private int _choiceIdx;
            private int _lastChoiceIdx;
            private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
          
            public bool Finished { get; private set; }

            public int GetNextThreadId(PriorityRelation priority, ThreadSet enabled, int numUnfinishedThreads)
            {
                if(numUnfinishedThreads == 1)
                {
                    int idx = 0;
                    while(!enabled[idx])
                    {
                        ++idx;
                    }
                    return idx; // out of bounds here implies deadlock, which should never happen.
                }
                Choice result;
                if (ResumeInProgress)
                {
                    result = _choices[_choiceIdx];
                    _choiceIdx++;
                }
                else
                {
                    var schedulable = priority.GetSchedulableThreads(enabled);
                    result = new Choice(schedulable);
                    _choices[_choiceIdx] = result;
                    _lastChoiceIdx++;
                    _choiceIdx++;
                }
                return result.Chosen;

            }

            public bool Advance()
            {
                _choiceIdx = 0;
                bool resuming = _lastChoiceIdx >= 0;
                while (_lastChoiceIdx >= 0 && _choices[_lastChoiceIdx].FullyExplored)
                {
                    _lastChoiceIdx--;
                }
                if (_lastChoiceIdx >= 0)
                {
                    _choices[_lastChoiceIdx].Next();
                }
                else if (resuming)
                {
                    Finished = true;
                }
                return !Finished;
            }
        }

        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;
        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
        public bool Finished { get; private set; }

        class ThreadSet 
        {
            private readonly bool[] _elems;
            public int NumElems { get; private set; }
            public ThreadSet(int numThreads)
            {
                _elems = new bool[numThreads];
                NumElems = 0;
            }

            public void Add(int idx)
            {
                NumElems++;
                _elems[idx] = true;
            }

            public void Remove(int idx)
            {
                NumElems--;
                _elems[idx] = false;
            }

            public bool this[int idx] => _elems[idx];
            
            public void Clear()
            {
                if(NumElems > 0)
                {
                    for(int i = 0; i < NumElems; ++i)
                    {
                        _elems[i] = false;
                    }
                }
            }

            public void ReplaceWith(ThreadSet other)
            {
                for(int i = 0; i < NumElems; ++i)
                {
                    _elems[i] = other._elems[i];
                }
                NumElems = other.NumElems;
            }

            public bool Contains(int idx)
            {
                return _elems[idx];
            }

            public ThreadSet Intersection(ThreadSet other)
            {
                return null; // TODO: This is a garbage promoting interface, do differently?
            }
        }

        class Choice
        {
            public int Chosen { get; private set; }
            public readonly ThreadSet _alternatives;
            public bool FullyExplored => _numElemsSeen >= _alternatives.NumElems;
            private int _numElemsSeen;

            public Choice(ThreadSet alternatives)
            {
                int n = alternatives.NumElems;
                if(n < 2)
                {
                    throw new ArgumentOutOfRangeException($"Choice cannot be made between {n} or fewer alternatives");
                }
            }

            public void Next()
            {
                if (FullyExplored)
                {
                    throw new Exception("Scheduling choice already exhausted.");
                }
                while(!_alternatives[Chosen] && Chosen < _alternatives.NumElems)
                {
                    Chosen++;
                }
                _numElemsSeen++;
            }
        }

        public bool NewIteration()
        {
            if(Finished)
            {
                throw new Exception("Already finished");
            }
            return _history.Advance();
        }
        private ThreadSet _finished;
        private ThreadSet _enabled;
        private ThreadSet[] _disabledSince;
        private ThreadSet[] _scheduledSince;
        private ThreadSet[] _enabledSince;
        private ThreadSet _availableToSchedule;
        private PriorityRelation _priority;
        private SchedulingHistory _history;


        private int _runningThreadIndex;
        private int NumUnfinishedThreads => _numThreads - _finished.NumElems;
        public bool AllThreadsFinished => _finished.NumElems == _numThreads;
        public int RunningThreadId { get; private set; }
        private readonly int _numThreads;

        public ExhaustiveScheduler(int numThreads, int maxChoices)
        {
            _numThreads = numThreads;
            _choices = new Choice[maxChoices]; 
            _choiceIdx = 0;
            _lastChoiceIdx = -1;
            MaybeSwitch();
        }

        public void MaybeSwitch()
        {
            if(AllThreadsFinished)
            {
                throw new Exception("All threads finished. Who called?");
            }
            // N.B. This only schedules enabled threads, a thread is re-enabled when the lock it is waiting on is released...
            // hence, need a lock-released
            RunningThreadId = _history.GetNextThreadId(_priority, _enabled, _numThreads - _finished.NumElems); 
            for(int i = 0; i < _numThreads; ++i)
            {
                _scheduledSince[i].Add(RunningThreadId);
            }
            _priority.RemovePriorityOf(RunningThreadId);
            return;
        }

        public bool ThreadWaiting(int waitingOnThreadId)
        {
            _enabled.Remove(RunningThreadId);
            _disabledSince[waitingOnThreadId].Add(RunningThreadId);
            _disabledBy[RunningThreadId] = waitingOnThreadId;
            for(int i = 0; i < _numThreads; ++i)
            {
                _enabledSince[i].Remove(RunningThreadId);
            }
            bool deadlock = _enabled.NumElems == 0; 
            return deadlock;
        } 

        public void ThreadFinishedWaiting()
        {
            _enabled.Add(RunningThreadId);
        }
    
        private readonly int[] _disabledBy;
        private const int InvalidThreadId = -1;

        public void LockReleased() // TODO: Need to actually call this!
        {
            for(int i = 0; i < _numThreads; ++i)
            {
                if(_disabledBy[i] == RunningThreadId)
                {
                    _disabledBy[i] = InvalidThreadId;
                    _enabled.Add(i);
                    _disabledSince[i].Remove(i); // Not certain this should be done, need to understand Theorem 1 properly first.
                }
            }
        }

        public void Yield()
        {
            _enabledSince[RunningThreadId].ReplaceWith(_enabled);
            _disabledSince[RunningThreadId].Clear();
            _scheduledSince[RunningThreadId].Clear();
            _priority.GivePriorityOver(RunningThreadId);
        }

        public void ThreadFinished() 
        {
            _finished.Add(RunningThreadId);
            if(!AllThreadsFinished)
            {
                MaybeSwitch();
            }
        }
    }
}