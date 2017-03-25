using System;   
using System.Collections.Generic;

namespace RelaSharp
{
    interface ITest 
    {
        IReadOnlyList<Action> ThreadEntries { get; }
        void OnFinished();
    }
}
