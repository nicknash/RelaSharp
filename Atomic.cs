using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class Atomic<T> 
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        protected InternalAtomic<T> _memoryOrdered;

        private void MaybeInit()
        {
            if(_memoryOrdered == null)
            {
                _memoryOrdered = new InternalAtomic<T>(TE.HistoryLength, TE.NumThreads);
                _memoryOrdered.Store(default(T), MemoryOrder.Relaxed, TE.RunningThread);
            }
        }
        
        protected ShadowThread Preamble()
        {
            MaybeInit();
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            return runningThread;
        }

        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Store(data, mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Store ({mo}) --> {Str(data)}");            
        }

        public T Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            var result = _memoryOrdered.Load(mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Load ({mo}): <-- {Str(result)}");
            return result;
        }

        public bool CompareExchange(T newData, T comparand, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            var runningThread = Preamble();
            T loadedData;
            var success = _memoryOrdered.CompareExchange(newData, comparand, mo, runningThread, out loadedData);
            var description = success ? $"Success: {Str(comparand)} == {Str(loadedData)}" : $"Failed: {Str(comparand)} != {Str(loadedData)}";
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"CompareExchange ({mo}): --> {Str(newData)} ({description})");
            return success;
        }

        public T Exchange(T newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            var runningThread = Preamble();
            var oldData = _memoryOrdered.Exchange(newData, mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Exchange ({mo}): --> {Str(newData)} ({Str(oldData)})");
            return oldData;
        }

        private static string Str(T y)
        {
            return y == null ? "null" : y.ToString();
        }

        public override string ToString()
        {
            return _memoryOrdered.CurrentValue.ToString();
        }
    }
 
    class CLRAtomic<T>
    {
        private Atomic<T> _atomic;

        public CLRAtomic()
        {
            _atomic = new Atomic<T>();
        }

        public T Read([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public T VolatileRead([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public void Write(T x, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // All writes are release in the CLR
            _atomic.Store(x, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }

        public void VolatileWrite(T x, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _atomic.Store(x, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        } 
    }    

    class CLRAtomic32
    {
        private Atomic32 _atomic;
        public CLRAtomic32()
        {
            _atomic = new Atomic32();
        }

        public int Read([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Load(MemoryOrder.Relaxed, memberName, sourceFilePath, sourceLineNumber);
        }

        public int VolatileRead([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Load(MemoryOrder.Acquire, memberName, sourceFilePath, sourceLineNumber);
        }

        public void Write(int x, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            // All writes are release in the CLR
            _atomic.Store(x, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        }

        public void VolatileWrite(int x, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            _atomic.Store(x, MemoryOrder.Release, memberName, sourceFilePath, sourceLineNumber);
        } 
        public int Add(int x, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Add(x, MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber);
        }

        public int Increment([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Increment(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber);
        }

        public int Decrement([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return _atomic.Decrement(MemoryOrder.SequentiallyConsistent, memberName, sourceFilePath, sourceLineNumber);
        }
    }

    class Atomic32 : Atomic<int>
    {
        public int Add(int x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            var result = _memoryOrdered.CurrentValue + x;
            _memoryOrdered.Exchange(result, mo, runningThread);
            return result;
        }

        public int Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Exchange(_memoryOrdered.CurrentValue + 1, mo, runningThread);
            return -9999;
        }

        public int Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Exchange(_memoryOrdered.CurrentValue - 1, mo, runningThread);
            return -9999;
        }
    }

    class Atomic64 : Atomic<long>
    {
        public long Add(long x, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            var result = _memoryOrdered.CurrentValue + x;
            _memoryOrdered.Exchange(result, mo, runningThread);
            return result;
        }

        public long Increment(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Exchange(_memoryOrdered.CurrentValue + 1, mo, runningThread);
            return -9999;
        }

        public long Decrement(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Exchange(_memoryOrdered.CurrentValue - 1, mo, runningThread);
            return -9999;
        }
    }


}

