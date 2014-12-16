using System;
using System.Diagnostics;

namespace SilentOrbit
{
    static class Debug
    {
        [Conditional("DEBUG")]
        public static void WriteLine(string s)
        {
#if DEBUG
            Console.Error.WriteLine(s);
#endif
        }
    }
}

