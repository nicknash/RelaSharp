using System.Runtime.CompilerServices;
using RelaSharp.MemoryModel;

namespace RelaSharp
{
    public class Atomic<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        internal InternalAtomic<T> _memoryOrdered; // TODO: Change this to "private protected" once C# 7.2 useable with .NET Core 2.0. 

        private void MaybeInit()
        {
            if(_memoryOrdered == null)
            {
                _memoryOrdered = new InternalAtomic<T>(TE.HistoryLength, TE.NumThreads, TE.Lookback);
                _memoryOrdered.Store(default(T), MemoryOrder.Relaxed, TE.RunningThread);
            }
        }
        
        protected void Preamble()
        {
            MaybeInit();
            TE.MaybeSwitch();
            var runningThread = TE.RunningThread;
            runningThread.IncrementClock();
            return;
        }

        public void Store(T data, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            _memoryOrdered.Store(data, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Store ({mo}) --> {Str(data)}");            
        }

        public T Load(MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            Preamble();
            var result = _memoryOrdered.Load(mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Load ({mo}) <-- {Str(result)}");
            return result;
        }

        public bool CompareExchange(T newData, T comparand, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            Preamble();
            T loadedData;
            var success = _memoryOrdered.CompareExchange(newData, comparand, mo, TE.RunningThread, out loadedData);
            var description = success ? $"Success: {Str(comparand)} == {Str(loadedData)}" : $"Failed: {Str(comparand)} != {Str(loadedData)}";
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"CompareExchange ({mo}): --> {Str(newData)} ({description})");
            return success;
        }

        public T Exchange(T newData, MemoryOrder mo, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0) 
        {
            Preamble();
            var oldData = _memoryOrdered.Exchange(newData, mo, TE.RunningThread);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Exchange ({mo}): --> {Str(newData)} ({Str(oldData)})");
            return oldData;
        }

        protected static string Str(T y)
        {
            return y == null ? "null" : y.ToString();
        }

        public override string ToString()
        {
            return _memoryOrdered.CurrentValue.ToString();
        }
    }
}

