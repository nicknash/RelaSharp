namespace RelaSharp.Scheduling
{
    interface ISchedulingAlgorithm
    {
        int GetNextThreadIndex(int numUnfinishedThreads);
        int WaitingGetThreadIndex(int runningThreadIndex, int numUnfinishedThreads);
        int YieldGetThreadIndex();
        bool NewIteration();
        bool Finished { get; }
    }
}