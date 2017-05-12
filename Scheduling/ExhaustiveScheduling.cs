using System;

namespace RelaSharp.Scheduling
{
    class ExhaustiveScheduling : ISchedulingAlgorithm
    {
        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;

        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
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

        public int GetNextThreadIndex(int numUnfinishedThreads)
        {
            if(numUnfinishedThreads == 1)
            {
                return 0;
            }
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
            return result.Chosen;
        }
    }
}