using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    class BoundedSPSCQueue : IRelaExample
    {
        private class Config
        {
            public readonly string Description;
            public readonly MemoryOrder EnqueueMemoryOrder;
            public readonly MemoryOrder DequeueMemoryOrder;
            public readonly bool UseEnqueueReleaseFence;
            public readonly bool UseDequeueAcquireFence;
            public readonly bool ExpectedToFail;

            public Config(string description, MemoryOrder enqueueMemoryOrder, MemoryOrder dequeueMemoryOrder, bool useEnqueueReleaseFence, bool useDequeueAcquireFence, bool expectedToFail)
            {
                Description = description;
                EnqueueMemoryOrder = enqueueMemoryOrder;
                DequeueMemoryOrder = dequeueMemoryOrder;
                UseEnqueueReleaseFence = useEnqueueReleaseFence;
                UseDequeueAcquireFence = useDequeueAcquireFence;
                ExpectedToFail = expectedToFail;
            }
        }
        private class Queue 
        {
            private MemoryOrdered<object>[] _data;
            private RaceChecked<int> _read = new RaceChecked<int>();
            private RaceChecked<int> _write = new RaceChecked<int>();
            private int _size;
            private Config _config;
            public Queue(int size, Config config)
            {
                _data = new MemoryOrdered<object>[size];
                for(int i = 0; i < size; ++i)
                {
                    _data[i] = new MemoryOrdered<object>();
                }
                _size = size;
                _config = config;
            }

            public bool Enqueue(object x)
            {
                var w = _write.Load();
                if(_data[w].Load(MemoryOrder.Relaxed) != null)
                {
                    return false;
                }
                if(_config.UseEnqueueReleaseFence)
                {
                    Fence.Insert(MemoryOrder.Release);
                }
                _data[w].Store(x, _config.EnqueueMemoryOrder);
                _write.Store((w + 1) % _size);
                return true;
            }

            public object Dequeue()
            {
                var r = _read.Load();
                var result = _data[r].Load(_config.DequeueMemoryOrder);
                if(_config.UseDequeueAcquireFence)
                {
                    Fence.Insert(MemoryOrder.Acquire);
                }
                if(result == null)
                {
                    return null;
                }
                _data[r].Store(null, MemoryOrder.Relaxed);
                _read.Store((r + 1) % _size);
                return result;
            }
        }
        public IReadOnlyList<Action> ThreadEntries { get; private set;}
        public string Name => "SPSC queue tests";
        public string Description => ActiveConfig.Description;
        public bool ExpectedToFail => ActiveConfig.ExpectedToFail;
        private Queue _queue;
        private IEnumerator<Config> _configs;
        private Config ActiveConfig => _configs.Current;
        private int _size;
        private static TestEnvironment TE = TestEnvironment.TE;

        public BoundedSPSCQueue()
        {
            ThreadEntries = new List<Action>{Producer, Consumer};
            _size = 3;
            var configList = new List<Config>{
                                               new Config("Enqueue and dequeue relaxed, no memory fences", MemoryOrder.Relaxed, MemoryOrder.Relaxed, false, false, true),
                                               new Config("Enqueue and dequeue relaxed, dequeue acquire fence only", MemoryOrder.Relaxed, MemoryOrder.Relaxed, false, true, true),
                                               new Config("Enqueue and dequeue relaxed, enqueue release fence only", MemoryOrder.Relaxed, MemoryOrder.Relaxed, true, false, true),
                                               new Config("Enqueue and dequeue relaxed, enqueue release and dequeue acquire fence", MemoryOrder.Relaxed, MemoryOrder.Relaxed, true, true, false),
                                               new Config("Enqueue release, dequeue relaxed, no memory fences", MemoryOrder.Release, MemoryOrder.Relaxed, false, false, true),
                                               new Config("Enqueue relaxed, dequeue acquire, no memory fences", MemoryOrder.Relaxed, MemoryOrder.Acquire, false, false, true),
                                               new Config("Enqueue release, dequeue acquire, no memory fences", MemoryOrder.Release, MemoryOrder.Acquire, false, false, false),
                                              };
            _configs = configList.GetEnumerator();
        }

        private void Producer()
        {
            for(int i = 0; i < _size; ++i)
            {
                var x = new MemoryOrdered<int>();
                x.Store(123 + i, MemoryOrder.Relaxed);
                _queue.Enqueue(x);
            }
        }
        private void Consumer()
        {
            int numDequeued = 0;
            while(numDequeued < _size)
            {
                var x = _queue.Dequeue();
                if(x != null)
                {
                    var y = (MemoryOrdered<int>) x;
                    var z = y.Load(MemoryOrder.Relaxed);
                    TE.Assert(z == 123 + numDequeued, $"Partially constructed object detected: value = {z}, numDequeued = {numDequeued}");
                    numDequeued++;
                }
            }
        }

        public void OnFinished()
        {

        }
        private void PrepareForNewConfig()
        {
            _queue = new Queue(_size, ActiveConfig);
        }
        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }
        public bool SetNextConfiguration()
        {
            bool moreConfigurations = _configs.MoveNext();
            PrepareForNewConfig();        
            return moreConfigurations;
        }
    }
    
}