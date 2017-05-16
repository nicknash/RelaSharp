namespace RelaSharp.Scheduling
{
   interface IScheduler
{
        void MaybeSwitch();
        bool ThreadWaiting();
        void ThreadFinishedWaiting();
        void Yield();
        bool NewIteration();
        int RunningThreadId { get; }
        bool AllThreadsFinished { get; }
    }    
}