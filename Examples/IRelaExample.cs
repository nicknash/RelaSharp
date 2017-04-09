namespace RelaSharp.Examples
{
    interface IRelaExample : IRelaTest
    {
        string Name { get; }
        string Description { get; }
        bool ExpectedToFail { get;}
        bool SetNextConfiguration();
        void PrepareForIteration();
    }
}