using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using RelaSharp.CLR;

namespace RelaSharp.Examples
{
    class COWList : IRelaExample
    {
        private static IRelaEngine RE = RelaEngine.RE;
       
        public string Name => "Multiple-writer copy-on-write list.";

        public string Description => $"{NumWritingThreads} writing threads, 1 reading thread.";

        public bool ExpectedToFail => false;

        public IReadOnlyList<Action> ThreadEntries { get; }

        private bool _moreConfigurations = true;

        private MultipleWriterCOWList _list;
        private const int NumWrittenPerThread = 10;
        private const int NumWritingThreads = 5;

        public COWList()
        {
            var writingThreads = Enumerable.Range(0, NumWritingThreads).Select(MakeWritingThread);
            ThreadEntries = new Action[]{ReadingThread}.Concat(writingThreads).ToList();
        }

        public void OnBegin()
        {
            _list = new MultipleWriterCOWList();
            for(int i = 0; i < NumWritingThreads; ++i)
            {
                _writingThreadFinished[i] = new Atomic<bool>();
            }
        }

        public void OnFinished()
        {
        }

        private Action MakeWritingThread(int threadIdx)
        {
            return () => WritingThread(threadIdx);
        }

        private Atomic<bool>[] _writingThreadFinished = new Atomic<bool>[NumWritingThreads];

        private void WritingThread(int threadIdx)
        {
            for(int i = 0; i < NumWrittenPerThread; ++i)
            {
                _list.Add(threadIdx * NumWrittenPerThread + i);
            }
            _writingThreadFinished[threadIdx].Store(true, MemoryOrder.SequentiallyConsistent);
        }

        private void ReadingThread()
        {
            while(true)
            {
                int i;
                for(i = 0; i < NumWritingThreads; ++i)
                {
                    if(!_writingThreadFinished[i].Load(MemoryOrder.SequentiallyConsistent))
                    {
                        break;
                    }
                }
                if(i == NumWritingThreads)
                {
                    break;
                }
            }
            var final = new List<int>();
            var numElems = NumWritingThreads * NumWrittenPerThread;
            RE.Assert(_list.Count == numElems, $"Expected final list to have {numElems} elements, but instead it has {_list.Count}");
            for(int i = 0; i < numElems; ++i)
            {
                final.Add(_list[i]);
            }
            final.Sort();
            for(int i = 0; i < numElems; ++i)
            {
                RE.Assert(final[i] == i, $"Expected sorted final list element number {i} to be {i}, but it's {final[i]}.");
            }
        }

        public void PrepareForIteration()
        {
        }
        public bool SetNextConfiguration()
        {
            var result = _moreConfigurations;
            _moreConfigurations = false;
            return result;
        }

        class MultipleWriterCOWList
        {
            private CLRAtomic<List<int>> _data;

            public int Count => RUnordered.Read(ref _data).Count;

            public MultipleWriterCOWList()
            {
                //RVolatile.Write(ref _data, new List<int>());
            }

            public void Add(int v)
            {
                while (true)
                {
                    var local = RVolatile.Read(ref _data);
                    var copy = local == null ? new List<int>() : new List<int>(local);
                    copy.Add(v);
                    if (RInterlocked.CompareExchange(ref _data, copy, local))
                    {
                        break;
                    }
                }
            }

            public int this[int idx] => RUnordered.Read(ref _data)[idx];
        }
    }
}