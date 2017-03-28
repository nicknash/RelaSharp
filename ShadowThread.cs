
namespace RelaSharp
{
    class ShadowThread
    {
        public long Clock => VC[Id];
        
        public readonly VectorClock VC; // TODO: Think about better names for these. "ReleasesAcquired" ?
        public readonly VectorClock Fenced; // TODO: Need separate acquire and release fences
        public readonly int Id;


        public ShadowThread(int id, int numThreads)
        {
            Id = id;
            VC = new VectorClock(numThreads);
            Fenced = new VectorClock(numThreads);
        }

        public void IncrementClock()
        {
            VC[Id]++;
        }
    }
}