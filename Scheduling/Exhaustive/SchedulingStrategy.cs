using System;

namespace RelaSharp.Scheduling.Exhaustive
{
    class SchedulingStrategy
    {
        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;
        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;
        private int[] _lookbacks;
        private Random _random;
        private int _lookBackIdx;
        public bool Finished { get; private set; }

        public SchedulingStrategy(ulong maxChoices)
        {
            _choices = new Choice[maxChoices]; 
            _lookbacks = new int[maxChoices];
            _random = new Random();
            _lookBackIdx = 0;
            _choiceIdx = 0;
            _lastChoiceIdx = -1;
        }

        public int GetNextThreadId(PriorityRelation priority, ThreadSet enabled)
        {
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

        public void Rollback()
        {
            _lastChoiceIdx--;
            _choiceIdx--;
        }

        public int GetLookback(int maxLookback)
        {
            // We ensure look-back is deterministic for resume purposes, but not exhaustive.
            if(ResumeInProgress)
            {
                return _lookbacks[_lookBackIdx++];
            }
            var lookback = _random.Next(maxLookback + 1);
            _lookbacks[_lookBackIdx++] = lookback;
            return lookback;
        }

        public int GetZeroLookback()
        {
            if(ResumeInProgress)
            {
                return _lookbacks[_lookBackIdx++];
            }
            _lookbacks[_lookBackIdx++] = 0;
            return 0;            
        }

        public bool Advance()
        {
            _choiceIdx = 0;
            _lookBackIdx = 0;
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
}