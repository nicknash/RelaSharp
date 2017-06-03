using System;
using RelaSharp.Scheduling.Exhaustive;

namespace RelaSharp.Scheduling
{ 
    class ExhaustiveScheduler : IScheduler
    {
        private readonly SchedulingStrategy _strategy;
        private readonly int _numThreads;
        private readonly int _yieldLookbackPenalty;

        private ThreadSet _finished;
        private ThreadSet _enabled;
        private ThreadSet[] _disabledSince;
        private ThreadSet[] _scheduledSince;
        private ThreadSet[] _enabledSince;
        private object[] _waitingOnLock;
        private PriorityRelation _priority;
        private int _currentYieldPenalty;
        public bool AllThreadsFinished => _finished.NumElems == _numThreads;
        public int RunningThreadId { get; private set; }

        public ExhaustiveScheduler(int numThreads, ulong maxChoices, int yieldLookbackPenalty)
        {
            _numThreads = numThreads;
            _strategy = new SchedulingStrategy(maxChoices);
            _yieldLookbackPenalty = yieldLookbackPenalty;
        }


        private void PrepareForScheduling()
        {
            _finished = new ThreadSet(_numThreads);
            _enabled = new ThreadSet(_numThreads);
            _disabledSince = new ThreadSet[_numThreads];
            _scheduledSince = new ThreadSet[_numThreads];
            _enabledSince = new ThreadSet[_numThreads];
            for(int i = 0; i < _numThreads; ++i)
            {
                _disabledSince[i] = new ThreadSet(_numThreads);
                _enabledSince[i] = new ThreadSet(_numThreads);
                _scheduledSince[i] = new ThreadSet(_numThreads);
                _enabled.Add(i);
            }
            _waitingOnLock = new object[_numThreads];
            _priority = new PriorityRelation(_numThreads);
            _currentYieldPenalty = 0;
        }

        public int ChooseLookback(int maxLookback)
        {
            return _currentYieldPenalty > 0 ? _strategy.GetZeroLookback() :  _strategy.GetLookback(maxLookback);
        }

        public bool NewIteration()
        {
            PrepareForScheduling();
            if(_strategy.Finished)
            {
                throw new Exception("Already finished");
            }
            bool result = _strategy.Advance();
            MaybeSwitch();
            return result;
        }

        public void MaybeSwitch()
        {
            if(AllThreadsFinished)
            {
                throw new Exception("All threads finished. Who called?");
            }
            if(_currentYieldPenalty > 0) 
            {            
                _currentYieldPenalty--;
            }
            ChooseRunningThread();
            SchedulingEpilogue();
            return;
        }

        private void ChooseRunningThread()
        {
            RunningThreadId = _strategy.GetNextThreadId(_priority, _enabled);             
        }
        private void SchedulingEpilogue()
        {
            for(int i = 0; i < _numThreads; ++i)
            {
                _scheduledSince[i].Add(RunningThreadId);
            }
            _priority.RemovePriorityOf(RunningThreadId);
        }

        public bool ThreadWaiting(int waitingOnThreadId, object lockObject)
        {
            // if waitingOnThreadId == -1, then we're waiting on a pulse...not held by any thread....
            // hence thread has disabled itself...shouldn't contribute to fairness algorithm
            _enabled.Remove(RunningThreadId);
            _disabledSince[waitingOnThreadId].Add(RunningThreadId);
            _waitingOnLock[RunningThreadId] = lockObject;
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

        public void LockReleased(object lockObject) 
        {
            for(int i = 0; i < _numThreads; ++i)
            {
                if(_waitingOnLock[i] == lockObject)
                {
                    _waitingOnLock[i] = null;
                    _enabled.Add(i);
                    _disabledSince[i].Remove(i); 
                }
            }
        }

        public void Yield()
        {
            // Give threads priority of this one that have been continuously enabled since
            // its last yield, or which have been disabled since it last yielded
            // Note, this is different from Musuvathi and Qadeer, because their _disabledSince 
            // is defined to include all threads disabled some some transition of the running thread
            // since its last yield. In reality, threads don't disable each other like this. CHESS 
            // implements this by having a lookahead which it searches for locks. So basically their condition is
            // (ContinuouslyEnabledSinceLastYield UNION DisabledByRunningThreadSinceLastYield) LESS ScheduledSinceLastYield
            // and mine is:
            // (ContinuouslyEnabledSinceLastYield LESS ScheduledSinceLastYield) UNION DisabledSinceLastYield
            // My 'DisabledSinceLastYield' includes the notion that the thread was disabled by the currently running thread, but note that
            // a thread can only be disabled by scheduling it, so something like Disabled LESS Scheduled would always be empty for my scheduler.
            // System.Console.WriteLine($"YIELD - {RunningThreadId}");
            var unfairlyStarved = _enabledSince[RunningThreadId];
            unfairlyStarved.LessWith(_scheduledSince[RunningThreadId]);
            unfairlyStarved.UnionWith(_disabledSince[RunningThreadId]);
            _priority.GivePriorityOver(RunningThreadId, unfairlyStarved);

            ChooseRunningThread();

            _enabledSince[RunningThreadId].ReplaceWith(_enabled);
            _disabledSince[RunningThreadId].Clear();
            _scheduledSince[RunningThreadId].Clear();
            SchedulingEpilogue();
            _currentYieldPenalty += _yieldLookbackPenalty;
        }

        public void ThreadFinished() 
        {
            _finished.Add(RunningThreadId);
            _enabled.Remove(RunningThreadId);
            _priority.RemovePriorityOf(RunningThreadId);
            if(!AllThreadsFinished)
            {
                MaybeSwitch();
            }
        }
    }
}