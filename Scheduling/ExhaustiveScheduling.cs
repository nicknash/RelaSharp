using System;

namespace RelaSharp.Scheduling
{
    class PriorityRelation
    {
        private bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.
        
        // Give y priority over x
        public void Prioritize(int x, int y)
        {

        }

        // Remove priority of x over all other threads
        public void Normalize(int x)
        {

        }

        public int[] GetSchedulableThreads(bool[] enabled)
        {

        }
    }

    class ExhaustiveScheduling : ISchedulingAlgorithm
    {
        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;

        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
     
        private bool[,] _hasPriority; // _hasPriority[x, y] = true <=> x will not be scheduled when y is enabled.
     
        public bool Finished { get; private set; }

        public ExhaustiveScheduling(ulong maxChoices)
        {
            _choices = new Choice[maxChoices]; 
            _choiceIdx = 0;
            _lastChoiceIdx = -1;
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

        public int WaitingGetThreadIndex(int runningThreadIndex, int numUnfinishedThreads)
        {
            if(numUnfinishedThreads < 2)
            {
                throw new Exception($"Undetected deadlock");
            }
            var result = GetNextChoice(numUnfinishedThreads);
            if(!ResumeInProgress && result.Chosen == runningThreadIndex)
            {
                result.Next();
            }
            return result.Chosen;
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

        public int YieldGetThreadIndex()
        {

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
    }
}