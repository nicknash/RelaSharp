namespace RelaSharp.Scheduling
{
    interface IScheduler
    {
        int RunningThreadId { get; }
        void MaybeSwitch();
        bool ThreadWaiting();
        void ThreadFinishedWaiting();
        void ThreadFinished();
    }
}