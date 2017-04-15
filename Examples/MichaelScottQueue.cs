using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class MichaelScottQueue : IRelaExample 
    {
        private class Config
        {
            public readonly string Description;
            public readonly int NumAddingThreads;
            public readonly int NumAddedPerThread;
            public readonly int NumRemovingThreads;
            public readonly bool AddAllBeforeRemove;
            public readonly Action PostCondition;
            public readonly bool ExpectedToFail;

            public Config(string description, int numAddingThreads, int numAddedPerThread, int numRemovingThreads, bool addAllBeforeRemove, Action postCondition, bool expectedToFail)
            {
                Description = description;
                NumAddingThreads = numAddingThreads;
                NumAddedPerThread = numAddedPerThread;
                NumRemovingThreads = numRemovingThreads;
                AddAllBeforeRemove = addAllBeforeRemove;
                PostCondition = postCondition;
                ExpectedToFail = expectedToFail;
            }
        }        
        private class Node 
        {
            public readonly int Value;
            public Atomic<Node> Next;
            public Node(int n)
            {
                Value = n;
                Next = new Atomic<Node>();
            }

            public override string ToString()
            {
                return $"{Value}"; 
            }    
        }
        private class NaiveLockFreeQueue
        {
            private Atomic<Node> _head;
            private Atomic<Node> _tail;
            public NaiveLockFreeQueue()
            {
                _head = new Atomic<Node>();
                _tail = new Atomic<Node>();
                var dummy = new Node(0);
                _head.Store(dummy, MemoryOrder.SequentiallyConsistent);
                _tail.Store(dummy, MemoryOrder.SequentiallyConsistent);
            }

            public void Enqueue(int n)
            {
                var newNode = new Node(n);
                while(true)
                {
                    var localTail = _tail.Load(MemoryOrder.SequentiallyConsistent); // TODO: review
                    var localTailNext = localTail.Next.Load(MemoryOrder.Acquire);

                    var tailNow = _tail.Load(MemoryOrder.Acquire); // TODO: Try .Relaxed here in sanity tests.
                    bool tailHasChanged = tailNow != localTail;
                    if(tailHasChanged)
                    {
                        continue;
                    }
                    bool tailIsBehind = localTailNext != null;
                    if (tailIsBehind)
                    {
                        // This happens if the first of the two CASes in the else branch of this if has 
                        // happened, but not the second. Here we'll correct the tail, causing
                        // that second CAS to fail in the other thread, but at least we won't have to wait for the other
                        // thread (i.e., we must do this to ensure the data structure is lock-free)
                        _tail.CompareExchange(localTailNext, localTail, MemoryOrder.AcquireRelease);
                    }
                    else
                    {
                        // If tail is still the true end of the queue, make its next pointer point at the newNode
                        if (tailNow.Next.CompareExchange(newNode, null, MemoryOrder.AcquireRelease))
                        {
                            // Now we need to try and get the tail back in sync. This is to maintain the at-most-one-behind
                            // invariant. If we fail, it's OK, someone else did this for us.
                            _tail.CompareExchange(newNode, tailNow, MemoryOrder.AcquireRelease);
                            return;
                        }
                    }
                }
            }

            public int? Dequeue()
            {
                while(true)
                {
                    var localHead = _head.Load(MemoryOrder.SequentiallyConsistent);
                    var localHeadNext = localHead.Next.Load(MemoryOrder.Acquire);
                    bool headHasChanged = localHead != _head.Load(MemoryOrder.Acquire); //  TODO: Try .Relaxed here in sanity tests.
                    if(headHasChanged)
                    {
                        continue;
                    }
                    if (localHeadNext == null)
                    {
                        return null;
                    }
                    else
                    {
                        if (_head.CompareExchange(localHeadNext, localHead, MemoryOrder.AcquireRelease))
                        {
                            return localHeadNext.Value;
                        }
                    }
                }
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }
        public string Name => "Michael-Scott Queue";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private NaiveLockFreeQueue _queue;
        private List<int> _dequeued;
        private Atomic<bool> _enqueuingThreadFinished;

        public MichaelScottQueue()
        {
            var configList = new List<Config>{
                new Config("1 threads enqueuing 5 elements, 1 non-interleaved thread dequeuing afterward.", 1, 5, 1, true, VerifyAllDequeuedInOrder, false),                
                new Config("1 thread enqueuing 10 element interleaved with 3 threads dequeing.", 1, 10, 3, false, VerifyAllDequeuedInOrder, false)};
            _configs = configList.GetEnumerator();
        }
        private void VerifyAllDequeuedInOrder()
        {
            int expectedNumRemoved = ActiveConfig.NumAddingThreads * ActiveConfig.NumAddedPerThread; 
            TE.Assert(_dequeued.Count == expectedNumRemoved, $"Incorrect data dequeued: expected {expectedNumRemoved} but found {_dequeued.Count}");
            for(int i = 0; i < _dequeued.Count; ++i)
            {
                TE.Assert(_dequeued[i] == i, $"Expected to find {i} in dequeued list but found {_dequeued[i]}.");
            }
        }

        private Action MakeEnqueuingThread(int threadIndex, int numPushedPerThread)
        {
            return () => PushingThread(threadIndex, numPushedPerThread);
        }

        private Action MakeDequeuingThread()
        {
            return PoppingThread; 
        }

        private void PushingThread(int threadIndex, int numAddedPerThread)
        {
            for (int i = 0; i < numAddedPerThread; ++i)
            {
                _queue.Enqueue(numAddedPerThread * threadIndex + i);
            }
            _enqueuingThreadFinished.Store(true, MemoryOrder.Release);
        }

        private void PoppingThread()
        {
            if(ActiveConfig.AddAllBeforeRemove)
            {
                while(!_enqueuingThreadFinished.Load(MemoryOrder.Acquire)) ;
            }
            while (_dequeued.Count < ActiveConfig.NumAddingThreads * ActiveConfig.NumAddedPerThread)
            {
                int? x = _queue.Dequeue();
                if (x.HasValue)
                {
                    _dequeued.Add(x.Value);
                }
            }
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }
        public void OnBegin()
        {
            _queue = new NaiveLockFreeQueue();
        }
 
        public void OnFinished()
        {
            ActiveConfig.PostCondition();
        }

        private void PrepareForNewConfig()
        {
            _dequeued = new List<int>();
            _enqueuingThreadFinished = new Atomic<bool>();
        }

        private void SetupActiveConfig()
        {
            var threadEntries = new List<Action>();
            for(int i = 0; i < ActiveConfig.NumAddingThreads; ++i)
            {
                threadEntries.Add(MakeEnqueuingThread(i, ActiveConfig.NumAddedPerThread));
            }
            for(int i = 0; i < ActiveConfig.NumRemovingThreads; ++i)
            {
                threadEntries.Add(MakeDequeuingThread());
            }
            ThreadEntries = threadEntries;                
        }
        
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            if(ActiveConfig != null)
            {
                SetupActiveConfig();
            }
            return moreConfigurations;
        }
    }
}
