namespace RelaSharp.Scheduling
{
    interface ISchedulingAlgorithm
    {
        int GetNextThreadIndex(int numUnfinishedThreads);
        void NewIteration();
        bool Finished { get; } // TODO: Give better name
    }
}