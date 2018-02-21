using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class TreiberStack : IRelaExample 
    {
        private class Config
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

        private class Node
        {
            public readonly int Value;
            public Atomic<Node> Next;
            public Node(int v)
            {
                Value = v;
                Next = new Atomic<Node>();
            }

            public override string ToString()
            {
                return $"{Value}"; 
            }
        }

        private class NaiveLockFreeStack
        {
            private Atomic<Node> _head;

            public NaiveLockFreeStack()
            {
                _head = new Atomic<Node>();
            }

            public void Push(int n)
            {
                Node currentHead;
                Node newHead = new Node(n);
                do 
                {
                    currentHead = _head.Load(MemoryOrder.Acquire); // TODO, add tests with these relaxed
                    newHead.Next.Store(currentHead, MemoryOrder.Relaxed);
                } while(!_head.CompareExchange(newHead, currentHead, MemoryOrder.AcquireRelease));
            }

            public int? Pop()
            {
                Node currentHead;
                Node newHead;
                int? result;
                do
                {
                    currentHead = _head.Load(MemoryOrder.Acquire); // TODO, add tests with this relaxed
                    result = currentHead?.Value;
                    newHead = currentHead?.Next.Load(MemoryOrder.Relaxed); // TODO, add tests with this relaxed
                } while(!_head.CompareExchange(newHead, currentHead, MemoryOrder.AcquireRelease));
                return result;
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set; }
        public string Name => "Treiber Stack";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static RelaEngine TR = RelaEngine.RE;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private NaiveLockFreeStack _stack;
        private Atomic<bool> _pushingThreadFinished;
        List<int> _popped;
        List<int> _poppedInOrder;

        public TreiberStack()
        {
            var configList = new List<Config>{new Config("3 threads pushing 5 elements each, interleaved with 2 threads popping", 3, 5, 2, false, VerifyAllPushedWerePopped, false), 
                                              new Config("1 thread pushing 20 elements each, interleaved with 10 threads popping", 1, 20, 10, false, VerifyAllPushedWerePopped, false),
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
            var distinctPopped = new HashSet<int>(_popped);
            TR.Assert(distinctPopped.Count == _popped.Count, "Duplicates popped!");
            for(int i = 0; i < ActiveConfig.NumPushingThreads * ActiveConfig.NumPushedPerThread; ++i)
            {
                TR.Assert(distinctPopped.Remove(i), $"Couldn't find {i} in data popped from stack");
            }
            TR.Assert(distinctPopped.Count == 0, "More data popped than pushed!");
        }

        private void VerifyAllPoppedInReverseOrder()
        {
            int expectedNumPushed = ActiveConfig.NumPushingThreads * ActiveConfig.NumPushedPerThread; 
            TR.Assert(_poppedInOrder.Count == expectedNumPushed, $"Too much data popped: expected {expectedNumPushed} but found {_poppedInOrder.Count}");
            for(int i = 0; i < expectedNumPushed; ++i)
            {
                int expected = expectedNumPushed - i - 1;
                TR.Assert(_poppedInOrder[i] == expected, $"Expected to find {expected} in popped list but found {_poppedInOrder[i]}.");
            }
        }
        public void OnBegin()
        {
            
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
            _popped = new List<int>();
            _poppedInOrder = new List<int>();
            _pushingThreadFinished = new Atomic<bool>();
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
