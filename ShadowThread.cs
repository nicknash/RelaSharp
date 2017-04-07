
namespace RelaSharp
{
    class ShadowThread
    {
        public long Clock => ReleasesAcquired[Id];
        public readonly VectorClock ReleasesAcquired; 
        public readonly VectorClock FenceReleasesToAcquire; 
        public readonly VectorClock FenceReleasesAcquired; 
        public readonly int Id;

        public ShadowThread(int id, int numThreads)
        {
            Id = id;
            ReleasesAcquired = new VectorClock(numThreads);
            FenceReleasesToAcquire = new VectorClock(numThreads);
            FenceReleasesAcquired = new VectorClock(numThreads);
        }

        public void IncrementClock()
        {
            ReleasesAcquired[Id]++;
        }
    }
}