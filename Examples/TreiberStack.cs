using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class TreiberStack : IRelaExample 
    {
        class Config
        {
            public readonly string Description;
            public readonly int NumPushingThreads;
            public readonly int NumPushedPerThread;
            public readonly int NumPoppingThreads;
            public readonly bool PushAllBeforePop;
            public readonly Action PostCondition;
            public readonly bool ExpectedToFail;

            public Config(string description, int numPushing, int numPushedPerThread, int numPopping, bool pushAllBeforePop, Action postCondition, bool expectedToFail)
            {
                Description = description;
                NumPushingThreads = numPushing;
                NumPushedPerThread = numPushedPerThread;
                NumPoppingThreads = numPopping;
                PushAllBeforePop = pushAllBeforePop;
                PostCondition = postCondition;
                ExpectedToFail = expectedToFail;
            }
        }

        class Node
        {
            public readonly int Value;
            public Node Next;
            public Node(int v)
            {
                Value = v;
            }

            public override string ToString()
            {
                return $"{Value}"; 
            }
        }

        class NaiveLockFreeStack
        {
            private MemoryOrdered<Node> _head;

            public NaiveLockFreeStack()
            {
                _head = new MemoryOrdered<Node>();
            }

            public void Push(int n)
            {
                Node currentHead;
                Node newHead = new Node(n);
                do 
                {
                    currentHead = _head.Load(MemoryOrder.Relaxed);
                    newHead.Next = currentHead;
                } while(!_head.CompareExchange(newHead, currentHead, MemoryOrder.AcquireRelease));
            }

            public int? Pop()
            {
                Node currentHead;
                int? result;
                do
                {
                    currentHead = _head.Load(MemoryOrder.Relaxed);
                    result = currentHead?.Value;
                } while(!_head.CompareExchange(currentHead?.Next, currentHead, MemoryOrder.AcquireRelease));
                return result;
            }
        }


        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Treiber Stack";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private NaiveLockFreeStack _stack;
        private MemoryOrdered<bool> _pushingThreadFinished;
        HashSet<int> _popped;
        List<int> _poppedInOrder;

        public TreiberStack()
        {
            var configList = new List<Config>{new Config("3 threads pushing 5 elements each, interleaved with 2 threads popping, threads interleaved.", 3, 5, 2, false, VerifyAllPushedWerePopped, false), 
                                              new Config("1 thread pushing 20 elements each, interleaved with 10 threads popping, threads interleaved.", 1, 20, 10, false, VerifyAllPushedWerePopped, false),
                                              new Config("1 thread pushing 30 elements, then 5 threads popping, all pushes before any pops.", 1, 30, 5, true, VerifyAllPoppedInReverseOrder, false)
                                             };
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        private void VerifyAllPushedWerePopped()
        {
            for(int i = 0; i < ActiveConfig.NumPushingThreads * ActiveConfig.NumPushedPerThread; ++i)
            {
                TE.Assert(_popped.Remove(i), $"Couldn't find {i} in data popped from stack");
            }
            TE.Assert(_popped.Count == 0, "More data popped than pushed!");
        }

        private void VerifyAllPoppedInReverseOrder()
        {
            int expectedNumPushed = ActiveConfig.NumPushingThreads * ActiveConfig.NumPushedPerThread; 
            TE.Assert(_poppedInOrder.Count == expectedNumPushed, $"Too much data popped: expected {expectedNumPushed} but found {_poppedInOrder.Count}");
            for(int i = 0; i < expectedNumPushed; ++i)
            {
                int expected = expectedNumPushed - i - 1;
                TE.Assert(_poppedInOrder[i] == expected, $"Expected to find {expected} in popped list but found {_poppedInOrder[i]}.");
            }
        }

        public void OnFinished()
        {
            ActiveConfig.PostCondition();
        }

        private Action MakePushingThread(int threadIndex, int numPushedPerThread)
        {
            return () => PushingThread(threadIndex, numPushedPerThread);
        }

        private Action MakePoppingThread()
        {
            return PoppingThread; 
        }

        private void PushingThread(int threadIndex, int numPushedPerThread)
        {
            for (int i = 0; i < numPushedPerThread; ++i)
            {
                _stack.Push(numPushedPerThread * threadIndex + i);
            }
            _pushingThreadFinished.Store(true, MemoryOrder.Release);
        }

        private void PoppingThread()
        {
            if(ActiveConfig.PushAllBeforePop)
            {
                while(!_pushingThreadFinished.Load(MemoryOrder.Acquire)) ;
            }
            while (_popped.Count < ActiveConfig.NumPushingThreads * ActiveConfig.NumPushedPerThread)
            {
                int? x = _stack.Pop();
                if (x.HasValue)
                {
                    _popped.Add(x.Value);
                    _poppedInOrder.Add(x.Value);
                }
            }
        }

        private void PrepareForNewConfig()
        {
            _stack = new NaiveLockFreeStack();
            _popped = new HashSet<int>();
            _poppedInOrder = new List<int>();
            _pushingThreadFinished = new MemoryOrdered<bool>();
        }        
        
        private void SetupActiveConfig()
        {
            var threadEntries = new List<Action>();
            for(int i = 0; i < ActiveConfig.NumPushingThreads; ++i)
            {
                threadEntries.Add(MakePushingThread(i, ActiveConfig.NumPushedPerThread));
            }
            for(int i = 0; i < ActiveConfig.NumPoppingThreads; ++i)
            {
                threadEntries.Add(MakePoppingThread());
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
