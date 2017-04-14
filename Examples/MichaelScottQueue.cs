using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class MichaelScottQueue : IRelaExample 
    {
        private class Node 
        {
            public readonly int Value;
            public Atomic<Node> Next;
            public Node(int n)
            {
                Value = n;
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
                _head.Store(dummy, MemoryOrder.Relaxed);
                _tail.Store(dummy, MemoryOrder.Relaxed);
            }

            public void Enqueue(int n)
            {
                var newNode = new Node(n);
                while(true)
                {
                    var localTail = _tail.Load(MemoryOrder.Relaxed);
                    var localTailNext = localTail.Next.Load(MemoryOrder.Relaxed);

                    var tailNow = _tail.Load(MemoryOrder.Acquire); // TODO: Try .Relaxed here in sanity tests.
                    if(tailNow == localTail)
                    {
                        bool tailIsBehind = localTailNext != null;
                        if(tailIsBehind)
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
                            if(tailNow.Next.CompareExchange(newNode, null, MemoryOrder.AcquireRelease))
                            {
                                // Now we need to try and get the tail back in sync. This is to maintain the at-most-one-behind
                                // invariant. If we fail, it's OK, someone else did this for us.
                                _tail.CompareExchange(newNode, tailNow, MemoryOrder.AcquireRelease);
                                return;
                            }
                        }
                    }
                }
            }

            public int? Dequeue()
            {
                while(true)
                {
                    var localHead = _head.Load(MemoryOrder.Relaxed);
                    var localHeadNext = localHead.Next.Load(MemoryOrder.Relaxed);
                    
                    if(localHead == _head.Load(MemoryOrder.Acquire)) //  TODO: Try .Relaxed here in sanity tests.
                    {
                        if(localHeadNext == null)
                        {
                            return null;
                        }
                        else
                        {
                            if(_head.CompareExchange(localHeadNext, localHead, MemoryOrder.AcquireRelease))
                            {
                                return localHead.Value;
                            }
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
    
        private IEnumerator<SimpleConfig> _configs;
        private SimpleConfig ActiveConfig => _configs.Current;

        public MichaelScottQueue()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            var configList = new List<SimpleConfig>{new SimpleConfig("All operations relaxed", MemoryOrder.Relaxed, true)};
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
        }

        public void Thread2()
        {
        }

        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
        }
        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
}
