using System;
using SilentOrbit.WebSockets;
using System.Net.Sockets;

namespace SilentOrbit.WebServer
{
    /// <summary>
    /// Creates sessions for the Listener
    /// Keeps a cache of old reusable session objects
    /// </summary>
    public abstract class HttpSessionManager
    {
        public abstract HttpSession GetSession();

        public abstract void SessionClosed(HttpSession httpSession);

        public abstract void SessionClosed(WebSocket socket);

        public abstract void UpgradeWebsocket(HttpSession httpSession, WebSocket socket);
    }
}

