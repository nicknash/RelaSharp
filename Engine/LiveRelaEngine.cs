using System.Threading;
using System.Runtime.CompilerServices;

namespace RelaSharp
{
    class LiveRelaEngine : IRelaEngine
    {
        public void Assert(bool shouldBeTrue, string reason, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
        }

        public void MaybeSwitch()
        {
        }

        public void Yield()
        {
            Thread.Yield();
        }
    }
}