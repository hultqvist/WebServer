
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SilentOrbit
{
    public static class Extensions
    {
        public static void NullSafeDispose(this IDisposable dis)
        {
            if (dis == null)
                return;

            dis.Dispose();
        }
    }
}
