using System;
using System.Collections.Generic;

namespace RelaSharp.Examples
{
    public class MinimalIPI : IRelaExample 
    {
        public IReadOnlyList<Action> ThreadEntries { get; private set; }

        public string Name => "Minimial Interprocessor Interrupt demonstration";
        public string Description => "Uses Interlocked.MemoryBarrierProcessWide (FlushProcessWriteBuffers / mprotect)";
        public bool ExpectedToFail => false;
        private static IRelaEngine RE = RelaEngine.RE;
        private Atomic<int> A;
        private Atomic<int> B;
        private bool _fastStoreDone;
        private bool _serializeDone;
        private bool _hasRun;

        public MinimalIPI()
        {
            ThreadEntries = new List<Action> {FastThread,SlowThread};
        }

        public void PrepareForIteration()
        {
            PrepareForNewConfig();
        }
    /* 
    From Dice et al:

First, we should more precisely state the requirements for SERIALIZE(t). 
Lets say the "Fast thread" F executes {ST A ; LD B} and the 
"Slow thread" S executes {ST B; MEMBAR; SERIALIZE(F); LD A;}.  
Typically, F would act as the BHT or JNI mutator, while
S would act in the role of the bias revoker or garbage collector.  

When Serialize(F) returns, one of the following invariants must hold:

  (A) If F has completed the {ST A} operation, then the value STed by 
      F into A will be visible to S by the time SERIALIZE(t) returns.
      That is, the {LD A} executed by S will observe the value STed 
      into A by F. 

  (B) If F has yet to complete the {ST A} operation, then 
      when F executes {LD B}, F will observe the value STed
      into B by S. 
    */

        // It seems to me that a simpler litmus test would just be: For both threads, upon completion, 
        // if the other thread has completed its store, then that store must be visible to this thread.
        public void SlowThread()
        {
            B.Store(1, MemoryOrder.Relaxed);
            Fence.InsertProcessWide();
            _serializeDone = true;
            var caseA = _fastStoreDone; // (A) Serialize done and F has completed its store => Must see F's store.
            if(caseA)
            {
                var seen = A.Load(MemoryOrder.Relaxed);
                RE.Assert(seen == 1, "Should see value stored by slow thread if it has done its store by the time serialize done.");
            }
        }

        public void FastThread()
        {
            var caseB = _serializeDone; // (B) Serialize done and F has yet to complete its store => Must see S's store (because that store was before serialize)
            A.Store(1, MemoryOrder.Relaxed);
            _fastStoreDone = true;
            if(caseB)
            {
                var seen = B.Load(MemoryOrder.Relaxed);
                RE.Assert(seen == 1, "Should see value stored by slow thread if it has stored & serialized before my (FastThread) store.");
            }
        }

        public void OnBegin()
        {
        }
        public void OnFinished()
        {
        }

        private void PrepareForNewConfig()
        {
            A = new Atomic<int>();
            B = new Atomic<int>();
            _fastStoreDone = false;
            _serializeDone = false;
        }

        public bool SetNextConfiguration()
        {
            bool oldHasRun = _hasRun;
            PrepareForNewConfig();
            _hasRun = true;
            return !oldHasRun;
        }
    }
}
