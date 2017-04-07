using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class MemoryOrdered<T> //where T : System.IEquatable<T> // TODO: restrict to atomic types.
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

        public static void Fence(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            TE.Scheduler();
            var runningThread = TE.RunningThread;
            runningThread.Fence(mo);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Fence: {mo}");
        }

        public override string ToString()
        {
            return _memoryOrdered.CurrentValue.ToString();
        }
/*
        public bool CompareExchange(T comparand, T newData, MemoryOrder mo)
        {
            //T currentData = // get
            if(!comparand.Equals(_memoryOrdered.CurrentValue)) // TODO: == 
            {
                // event log ?
                return false;
            }
            // TODO: Implement successful path
            return true;
        }*/
    }
}

