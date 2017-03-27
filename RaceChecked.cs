namespace RelaSharp
{
    class RaceChecked<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        private InternalRaceChecked<T> _raceChecked;
       
        public void Store(T data)
        {
            MaybeInit();
            _raceChecked.Store(data, TE.RunningThread);
        }

        public T Load()
        {
            MaybeInit();
            return _raceChecked.Load(TE.RunningThread);
        }

        private void MaybeInit()
        {
            if(_raceChecked == null)
            {
                _raceChecked = new InternalRaceChecked<T>(TE.NumThreads);
                _raceChecked.Store(default(T), TE.RunningThread);
            }
        }
    }    
}