namespace RelaSharp.Scheduling
{
    interface IScheduler
    {
        void MaybeSwitch();
        bool ThreadWaiting();
        void ThreadFinishedWaiting();
        void ThreadFinished();
    }
}