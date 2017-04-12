using System.Runtime.CompilerServices;

namespace RelaSharp
{
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
        
        private ShadowThread Preamble()
        {
            MaybeInit();
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            return runningThread;
        }

        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            _memoryOrdered.Store(data, mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Store ({mo}) --> {data}");            
        }

        public T Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            var runningThread = Preamble();
            var result = _memoryOrdered.Load(mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Load ({mo}): <-- {result}");
            return result;
        }

        public bool CompareExchange(T newData, T comparand, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            var runningThread = Preamble();
            T loadedData;
            var success = _memoryOrdered.CompareExchange(newData, comparand, mo, runningThread, out loadedData);
            var description = success ? $"Success: {comparand} == {loadedData}" : $"Failed: {comparand} != {loadedData}";
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"CompareExchange ({mo}): --> {newData} ({description})");
            return success;
        }

        public T Exchange(T newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            var runningThread = Preamble();
            var oldData = _memoryOrdered.Exchange(newData, mo, runningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Exchange ({mo}): --> {newData} ({oldData})");
            return oldData;
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

