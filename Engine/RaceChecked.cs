using System.Runtime.CompilerServices;
using RelaSharp.MemoryModel;

namespace RelaSharp
{
    
    public class RaceChecked<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private InternalRaceChecked<T> _raceChecked;
       
        public void Store(T data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            var runningThread = TE.RunningThread;
            _raceChecked.Store(data, TE.RunningThread, TE.FailTest);
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Store --> {data}");
        }

        public T Load([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            MaybeInit();
            var result = _raceChecked.Load(TE.RunningThread, TE.FailTest); 
            TE.RecordEvent(memberName, sourceFilePath, sourceLineNumber, $"Load <-- {result}");
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