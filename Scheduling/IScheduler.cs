namespace RelaSharp.Scheduling
{
   interface IScheduler
{
        void MaybeSwitch();
        
        bool ThreadWaiting(); // N.B. This will likely have to change to informing when a lock held/released, exhaustive scheduler will need to know about enabled->disabled transitions without scheduling thread.
        void ThreadFinishedWaiting(); 
        void Yield();
        bool NewIteration();
        int RunningThreadId { get; }
        bool AllThreadsFinished { get; }
    }    
}