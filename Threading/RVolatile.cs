using System.Runtime.CompilerServices;

namespace RelaSharp
{
    static class RVolatile
    {
        public static T Read<T>(ref CLRAtomic<T> data, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            return data.VolatileRead(memberName, sourceFilePath, sourceLineNumber);
        }

        public static void Write<T>(ref CLRAtomic<T> data, T newValue, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            if(data == null)
            {
                data = new CLRAtomic<T>();
            }
            data.VolatileWrite(newValue, memberName, sourceFilePath, sourceLineNumber);
        }
    }
}