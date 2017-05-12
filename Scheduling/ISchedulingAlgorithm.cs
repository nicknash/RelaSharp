namespace RelaSharp.Scheduling
{
    interface ISchedulingAlgorithm
    {
        int GetNextThreadIndex(int numUnfinishedThreads);
        bool NewIteration();
        bool Finished { get; }
    }
}