using System;
using RelaSharp.Scheduling.Exhaustive;

namespace RelaSharp.Scheduling
{ 
    public class ExhaustiveScheduler : IScheduler
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
        private bool _deadlock;

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
            _deadlock = false;
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
            _strategy.Rollback();
            _enabled.Remove(RunningThreadId);
            // If waitingOnThreadId == -1, then the running thread has blocked itself waiting on a condition.
            // So there isn't a clear notion of which thread it is waiting on, or has been disabled since the
            // last yield of. It might make sense to add the running thread to all the _disableSince sets
            // rather than none, but I'm not going to explore this scheduling more deeply, at least for now.
            bool threadWaitingOnCondition = waitingOnThreadId == -1;
            if(!threadWaitingOnCondition)
            {
                _disabledSince[waitingOnThreadId].Add(RunningThreadId);
            }
            _waitingOnLock[RunningThreadId] = lockObject;
            for(int i = 0; i < _numThreads; ++i)
            {
                _enabledSince[i].Remove(RunningThreadId);
            }
            _deadlock = _enabled.NumElems == 0; 
            if(!_deadlock)
            {
                MaybeSwitch();
            }
            return _deadlock;
        } 

        public void ThreadFinishedWaiting()
        {
            _enabled.Add(RunningThreadId);
            _waitingOnLock[RunningThreadId] = null;
        }

        public void LockReleased(object lockObject) 
        {
            for(int i = 0; i < _numThreads; ++i)
            {
                // Re-enabling all threads waiting on the lock allows spurious wake-ups for threads
                // that have waited on a condition variable (i.e. RMonitor.Wait in CLR land).
                // I think this is fine, as it at worst it makes running inside the model checker a potentially harsher environment
                // than outside.
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
            // This is the core of the algorithm, inspired by Musuvathi and Qadeer's "Fair Stateless Model Checking" from PLDI'08, which is the basis 
            // of the CHESS scheduler.
            //
            // The rationale is as follows: Give threads priority of the currently running thread that have been continuously enabled since
            // its last yield, or which have been disabled since it last yielded
            // Note, this is different from Musuvathi and Qadeer, because their _disabledSince 
            // is defined to include all threads disabled during some transition of the running thread
            // since its last yield. In reality, threads don't disable each other like this. From looking at the CHESS source it appears it 
            // implements this by having a lookahead which it searches for locks. So basically their condition is
            // (ContinuouslyEnabledSinceLastYield UNION DisabledByRunningThreadSinceLastYield) LESS ScheduledSinceLastYield
            // and mine is:
            // (ContinuouslyEnabledSinceLastYield LESS ScheduledSinceLastYield) UNION DisabledSinceLastYield
            // My 'DisabledSinceLastYield' includes the notion that the thread was disabled by the currently running thread, but note that
            // a thread can only be disabled by scheduling it, so something like Disabled LESS Scheduled would always be empty for my scheduler.
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
                if(_deadlock)
                {
                    // If there is a deadlock, manually schedule the threads
                    // in turn, so they have the opportunity to throw an exception and exit.
                    // Calling MaybeSwitch() here isn't possible, because there are no 
                    // enabled threads to schedule.
                    var unfinished = new ThreadSet(_numThreads);
                    for(int i  = 0; i < _numThreads; ++i)
                    {
                        if(!_finished[i])
                        {
                            RunningThreadId = i;
                        }
                    }
                }
                else
                {
                    MaybeSwitch();
                }
            }
        }
    }
}