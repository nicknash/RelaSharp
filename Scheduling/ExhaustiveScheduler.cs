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

        class SchedulingHistory
        {
            private Choice[] _choices;
            private int _choiceIdx;
            private int _lastChoiceIdx;
            private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
          
            public bool Finished { get; private set; }

            public int GetNextThreadId(PriorityRelation priority, ThreadSet enabled)
            {
                if (numUnfinishedThreads == 1) 
                {
                    return 0;
                }
                Choice result;
                if (ResumeInProgress)
                {
                    result = _choices[_choiceIdx];
                    _choiceIdx++;
                }
                else
                {
                    result = new Choice(numUnfinishedThreads);
                    _choices[_choiceIdx] = result;
                    _lastChoiceIdx++;
                    _choiceIdx++;
                }
                return result;

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
            private int _numElems;
            public ThreadSet(int numThreads)
            {
                _elems = new bool[numThreads];
                _numElems = 0;
            }

            public void Add(int idx)
            {
                _numElems++;
                _elems[idx] = true;
            }

            public void Remove(int idx)
            {
                _numElems--;
                _elems[idx] = false;
            }

            public void Clear()
            {
                if(_numElems > 0)
                {
                    for(int i = 0; i < _numElems; ++i)
                    {
                        _elems[i] = false;
                    }
                }
            }

            public bool Contains(int idx)
            {
                return _elems[idx];
            }
        }

        class Choice
        {
            public int Chosen { get; private set; }
            public readonly int NumAlternatives;
            public bool FullyExplored => Chosen >= NumAlternatives - 1;

            public Choice(int numAlternatives)
            {
                if(numAlternatives < 2)
                {
                    throw new ArgumentOutOfRangeException($"Choice cannot be made between {numAlternatives} or fewer alternatives");
                }
                NumAlternatives = numAlternatives;
                Chosen = 0;
            }

            public void Next()
            {
                if (FullyExplored)
                {
                    throw new Exception("Scheduling choice already exhausted.");
                }
                Chosen++;
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
        private int NumUnfinishedThreads =>_unfinishedThreadIds.NumElems;
        public bool AllThreadsFinished => _unfinishedThreadIds.NumElems == 0;
        public int RunningThreadId { get; private set; }
        private int _numThreads;

        public ExhaustiveScheduler(int numThreads, int maxChoices)
        {
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
            RunningThreadId = _history.GetNextThreadId(_priority, _enabled);  
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
            for(int i = 0; i < _numThreads; ++i)
            {
                _enabledSince[i].Remove(RunningThreadId);
            }
            // if a
            return deadlock;
        } 

        public void ThreadFinishedWaiting()
        {
            _enabled.Add(RunningThreadId);
        }
    
        public void Yield()
        {
            _enabledSince[RunningThreadId].BuildFrom(_enabled);
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