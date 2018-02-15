using System;   
using System.Collections.Generic;

namespace RelaSharp
{
    public interface IRelaTest 
    {
        IReadOnlyList<Action> ThreadEntries { get; }
        void OnBegin();
        void OnFinished();
    }
}
