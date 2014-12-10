using System;
using System.Net.Sockets;
using System.Net;

namespace SilentOrbit.WebServer
{
	/// <summary>
	/// Fix the flushing of Nagle's algorithm
	/// </summary>
	public class WebStream : NetworkStream
	{
		public WebStream(Socket s) : base(s, true)
		{
			RemoteEndPoint = (IPEndPoint)s.RemoteEndPoint;
		}

		public IPEndPoint RemoteEndPoint { get; private set; }

		public override void Flush()
		{
			Socket.NoDelay = true;
			Socket.NoDelay = false;
		}

		public bool NoDelay
		{
			get { return Socket.NoDelay; }
			set { Socket.NoDelay = value; }
		}

		public void Shutdown(SocketShutdown direction)
		{
			if (Socket == null)
				return;
			Socket.Shutdown(direction);
		}

		public LingerOption LingerState { get { return Socket.LingerState; } }
	}
}

