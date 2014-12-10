using System;
using SilentOrbit.WebServer;
using SilentOrbit.WebSockets;

namespace ExampleServer
{
	public class ExampleSessionManager : HttpSessionManager
	{
		public override HttpSession GetSession ()
		{
			return new ExampleSession ();
		}

		public override void SessionClosed (HttpSession httpSession)
		{
		}

		public override void UpgradeWebsocket (HttpSession httpSession, WebSocket socket)
		{
			throw new NotImplementedException ();
		}

		public override void SessionClosed (WebSocket socket)
		{
			throw new NotImplementedException ();
		}
	}
}

