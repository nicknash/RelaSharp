using System;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var test = new PetersenTest();
            TestRunner.Run(test);
        }
    }

    public class TestRunner 
    {
        private static TestEnvironment TE = TestEnvironment.TE;

        public static void Run(ITest test)
        {
            test.Setup();
            TE.RunningTest = test;
            int runningThread = 0;
            TE.Scheduler();
        }
    }

    public class TrackingData
    {

    }

    public class TestEnvironment
    {
        public static TestEnvironment TE;

        public ITest RunningTest;
    
        private int _runningThread;
        private int _numThreads;

        public TrackingData NewMemOrdered()
        {
            return null;
        }

        public void Scheduler()
        {
            _runningThread = (_runningThread + 1) % _numThreads;
            RunningTest.Run(_runningThread);
        }
    }

    interface ITest 
    {
        int NumThreads { get; }
        void Setup();
        void RunThread(int index);

    }

    public class PetersenTest : IThread 
    {
        private MemOrdered<int> flag0;
        private MemOrdered<int> flag1;
        private MemOrdered<int> turn;
        
        public int NumThreads => 2;

        void Setup()
        {
        //     flag0 = 0;
        //     flag1 = 0;
        //     turn = 0;
        }

        void RunThread(int index)
        {
            if(index == 0)
            {

            }
        }
    }

    public struct MemOrdered<T>
    {
        private static TestEnvironment TE = TestEnvironment.TE;
        private T _data;
        private TrackingData trackingData;

        public MemOrdered<T>(T data)
        {
            _trackingData = TestEnvironment.TE.newMemOrdered()
        
        }

        public void Store()
        {
            TE.scheduler();
        }
    }
}
