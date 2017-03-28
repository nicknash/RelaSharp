using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class ExecutionEvent  // Stub implementation for now
    {
        private string _callingThreadFuncName;
        private string _sourceFilePath;
        private int _sourceLineNumber;
        private string _eventInfo;

        public ExecutionEvent(string callingThreadFuncName, string sourceFilePath, int sourceLineNumber, string eventInfo)
        {
            _callingThreadFuncName = callingThreadFuncName;
            _sourceFilePath = sourceFilePath;
            _sourceLineNumber = sourceLineNumber;
            _eventInfo = eventInfo;
        }

        public override string ToString()
        {
            return $"{_sourceFilePath}:{_callingThreadFuncName}:{_sourceLineNumber}:{_eventInfo}";
        }
    }
    
    class RaceChecked<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private InternalRaceChecked<T> _raceChecked;
       
        public void Store(T data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            var runningThread = TE.RunningThread;
            _raceChecked.Store(data, TE.RunningThread, TE.FailTest);
            runningThread.AddEvent(new ExecutionEvent(memberName, sourceFilePath, sourceLineNumber, $"Store: {data}"));
        }

        public T Load([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            var runningThread = TE.RunningThread;
            var result = _raceChecked.Load(runningThread, TE.FailTest); 
            runningThread.AddEvent(new ExecutionEvent(memberName, sourceFilePath, sourceLineNumber, $"Load: {result}"));
            return result; 
        }

        private void MaybeInit()
        {
            if(_raceChecked == null)
            {
                _raceChecked = new InternalRaceChecked<T>(TE.NumThreads);
                _raceChecked.Store(default(T), TE.RunningThread, TE.FailTest);
            }
        }
    }    
}