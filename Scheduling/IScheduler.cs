namespace RelaSharp.Scheduling
{
    interface IScheduler
    {
        void MaybeSwitch();
        bool ThreadWaiting(int waitingOnThreadId, object lockObject);
        void ThreadFinishedWaiting();
        void ThreadFinished();
        void LockReleased(object lockObject);
        void Yield();
        bool NewIteration();
        int RunningThreadId { get; }
        bool AllThreadsFinished { get; }
    }
}