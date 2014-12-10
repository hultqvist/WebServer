using System;
using SilentOrbit.WebServer;
using System.Net;

namespace ExampleServer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Example Server!");

			var manager = new ExampleSessionManager ();

			int port = 8080;

			var listener = new Listener (IPAddress.Loopback, port, manager);

			Console.WriteLine ("Visit the server at http://localhost:" + port);

			listener.Run ();
		}
	}
}
