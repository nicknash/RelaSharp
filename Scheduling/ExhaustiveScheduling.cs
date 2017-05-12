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

        public ExhaustiveScheduling()
        {
            _choices = new Choice[10000]; // TODO: bound by live lock limit. 
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

        public void NewIteration()
        {
            if(Finished)
            {
                throw new Exception("Already finished");
            }
            _choiceIdx = 0;
            if(ResumeInProgress)
            {
                var choice = _choices[_lastChoiceIdx];
                choice.Next();
            }
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
                //Console.WriteLine($"REPLAY: last choice idx = {_lastChoiceIdx}, current choice idx = {_choiceIdx}");
                result = _choices[_choiceIdx];
                _choiceIdx++;
                if (result.FullyExplored)
                {
                    Finished = _lastChoiceIdx == 0;
                    _choices[_lastChoiceIdx] = null;
                    _lastChoiceIdx--;
                    _choiceIdx--;
                }
            }
            else
            {
                //Console.WriteLine($"ADDING: {numUnfinishedThreads}, choice idx {_choiceIdx}, last choice idx = {_lastChoiceIdx}");
                result = new Choice(numUnfinishedThreads);
                _choices[_choiceIdx] = result;
                _lastChoiceIdx++;                
                _choiceIdx++;
            }
            return result.Chosen;
        }
    }
}