using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class InvalidAPIUseException : System.Exception  
    {

    }

    class MemoryOrdered<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private InternalMemoryOrdered<T> _memoryOrdered;

        private void MaybeInit()
        {
            if(_memoryOrdered == null)
            {
                _memoryOrdered = new InternalMemoryOrdered<T>(TE.HistoryLength, TE.NumThreads);
                _memoryOrdered.Store(default(T), MemoryOrder.Relaxed, TE.RunningThread);
            }
        }
        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            _memoryOrdered.Store(data, mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Store ({mo}) --> {data}");            
        }

        public T Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            var result = _memoryOrdered.Load(mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Load ({mo}): <-- {result}");
            return result;
        }

        public bool CompareExchange(T comparand, T newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            MaybeInit();
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            var success = _memoryOrdered.CompareExchange(comparand, newData, mo, runningThread);
            // TODO: TE.RecordEvent(memberName, sor)
            return success;
        }

        public T Exchange(T newData, MemoryOrder mo)
        {
            return default(T);
        }

        public override string ToString()
        {
            return _memoryOrdered.CurrentValue.ToString();
        }
    }
}

