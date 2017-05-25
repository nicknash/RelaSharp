namespace RelaSharp.Scheduling.Exhaustive
{
    class SchedulingStrategy
    {
        private Choice[] _choices;
        private int _choiceIdx;
        private int _lastChoiceIdx;
        private bool ResumeInProgress => _choiceIdx <= _lastChoiceIdx;

        public bool Finished { get; private set; }

        public SchedulingStrategy(ulong maxChoices)
        {
            _choices = new Choice[maxChoices]; 
            _choiceIdx = 0;
            _lastChoiceIdx = -1;
        }

        public int GetNextThreadId(PriorityRelation priority, ThreadSet enabled, int numUnfinishedThreads)
        {
            if (numUnfinishedThreads == 1)
            {
                int idx = 0;
                while (!enabled[idx])
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
}