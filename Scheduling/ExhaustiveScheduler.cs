using System;

namespace RelaSharp.Scheduling
{ 
    class ExhaustiveScheduler : IScheduler
    {
        class PriorityRelation
        {
            private readonly int _numThreads;
            private readonly bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.

            public PriorityRelation(int numThreads)
            {
                _numThreads = numThreads;
                _hasPriority = new bool[numThreads, numThreads];
            }

            // Give all threads priority over x
            public void GivePriorityOver(int x)
            {
                for(int i = 0; i < _numThreads; ++i)
                {
                    _hasPriority[x, i] = true;
                }
            }

            // Remove priority of x over all threads.
            public RemovePriorityOf(int x)
            {
                for(int i = 0; i < _numThreads; ++i)
                {
                    _hasPriority[i, x] = false;
                }                
            }

            public int[] GetSchedulableThreads(bool[] enabled)
            {

            }
        }

        class SchedulingHistory
        {
            private Choice[] _choices;
            private int _choiceIdx;
            private int _lastChoiceIdx;
            private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
          
            public int GetNextThreadId(ThreadSet availableToSchedule)
            {

            }

            public bool Advance()
            {

            }

            public void Rewind()
            {

            }
        }

        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;

        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
     
        private bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.
     
        public bool Finished { get; private set; }

        class ThreadSet 
        {
            public void Add(int idx)
            {

            }

            public void Remove(int idx)
            {

            }

            public void Clear()
            {

            }

            public bool Contains(int idx)
            {

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
            _choiceIdx = 0;
            bool resuming = _lastChoiceIdx >= 0;
            while (_lastChoiceIdx >= 0 && _choices[_lastChoiceIdx].FullyExplored)
            {
                _lastChoiceIdx--;
            }
            if(_lastChoiceIdx >= 0)
            {
                _choices[_lastChoiceIdx].Next();
            }
            else if(resuming)
            {
                Finished = true;
            }
            return !Finished;
        }

        public int GetNextThreadIndex(int numUnfinishedThreads)
        {
            if(numUnfinishedThreads == 1)
            {
                return 0;
            }
            var choice = GetNextChoice(numUnfinishedThreads); 
            return choice.Chosen;
        }

        private Choice GetNextChoice(int numUnfinishedThreads)
        {
            Choice result;
            if(ResumeInProgress)
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
            RunningThreadId = _history.GetNextThreadId(_availableToSchedule);  
            for(int i = 0; i < _numThreads; ++i)
            {
                _scheduledSince[i].Add(RunningThreadId);
            }
            // Remove priority of selected thread.
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