using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class TreiberStack : IRelaExample 
    {
        class Config
        {
            public readonly string Description;

            public readonly bool ExpectedToFail;

            public Config(string description, bool expectedToFail)
            {
                Description = description;
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
                return $"{Value}"; // actually get mem address would be clearer.
            }
        }

        class LockFreeStack
        {
            private MemoryOrdered<Node> _head;

            public LockFreeStack()
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

            public int Pop()
            {
                Node currentHead;
                int result;
                do
                {
                    currentHead = _head.Load(MemoryOrder.Relaxed);
                    result = currentHead.Value;
                } while(!_head.CompareExchange(currentHead.Next, currentHead, MemoryOrder.AcquireRelease));
                return result;
            }

            public bool IsEmpty => _head.Load(MemoryOrder.Acquire) == null;
        }


        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Treiber Stack";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private static TestEnvironment TE = TestEnvironment.TE;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private LockFreeStack _stack;

        int[] _pushed = new int[]{1,2,3,4,5};
        List<int> _popped;

        public TreiberStack()
        {
            ThreadEntries = new List<Action> {Thread1,Thread2};
            // TODO: Make configs with several threads pushing/popping.
            var configList = new List<Config>{new Config("One thread pushing, one thread popping", false), 
                                             };
            _configs = configList.GetEnumerator();
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }

        public void Thread1()
        {
           for(int i = 0; i < _pushed.Length; ++i)
           {
               _stack.Push(i);
           }        
        }

        public void Thread2()
        {
            int numPopped = 0;
            while(numPopped < _pushed.Length)
            {
                if(!_stack.IsEmpty)
                {
                    _popped.Add(_stack.Pop());
                    ++numPopped;
                }
            }
        }

        public void OnFinished()
        {
            for(int i = 0; i < _popped.Count; ++i)
            {
                Console.Write(_popped[i] + " ");
            }
            Console.WriteLine();
            TE.FailTest("Force failure for testing");
        }

        private void PrepareForNewConfig()
        {
            _stack = new LockFreeStack();
            _popped = new List<int>();
        }

        public bool SetNextConfiguration()
        {
            PrepareForNewConfig();
            bool moreConfigurations = _configs.MoveNext();
            return moreConfigurations;
        }
    }
}
