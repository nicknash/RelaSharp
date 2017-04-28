using System;

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

        public void Fence(MemoryOrder mo, VectorClock sequentiallyConsistentFence)
        {
            switch(mo) // TODO: Should this be done in Fence.Insert ? 
            {
                case MemoryOrder.Acquire:
                    AcquireFence();
                break;
                case MemoryOrder.Release:
                    ReleaseFence();
                break;
                case MemoryOrder.AcquireRelease:
                    AcquireFence();
                    ReleaseFence();
                break;
                case MemoryOrder.SequentiallyConsistent:
                    AcquireFence();
                    sequentiallyConsistentFence.Join(ReleasesAcquired);
                    ReleasesAcquired.Assign(sequentiallyConsistentFence);
                    ReleaseFence();
                break;
                default:
                    throw new Exception($"Unsupported memory fence order {mo}");
            }
        }

        private void AcquireFence()
        {
            ReleasesAcquired.Join(FenceReleasesToAcquire); 
        }
        private void ReleaseFence()
        {
            FenceReleasesAcquired.Assign(ReleasesAcquired);
        }
        private void AcquireReleaseFence()
        {
            AcquireFence();
            ReleaseFence();
        }
        public void IncrementClock()
        {
            ReleasesAcquired[Id]++;
        }
    }
}