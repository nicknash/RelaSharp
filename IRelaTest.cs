using System;   
using System.Collections.Generic;

namespace RelaSharp
{
    interface IRelaTest 
    {
        IReadOnlyList<Action> ThreadEntries { get; }
        void OnBegin();
        void OnFinished();
    }
}
