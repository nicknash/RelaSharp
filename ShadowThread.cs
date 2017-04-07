
namespace RelaSharp
{
    class ShadowThread
    {
        public long Clock => ReleasesAcquired[Id];
        
        public readonly VectorClock ReleasesAcquired; 
        public readonly VectorClock AcquireFence; 
        public readonly VectorClock ReleaseFence; 

        public readonly int Id;

        public ShadowThread(int id, int numThreads)
        {
            Id = id;
            ReleasesAcquired = new VectorClock(numThreads);
            AcquireFence = new VectorClock(numThreads);
            ReleaseFence = new VectorClock(numThreads);
        }

        public void IncrementClock()
        {
            ReleasesAcquired[Id]++;
        }
    }
}