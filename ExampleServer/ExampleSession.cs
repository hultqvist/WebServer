using System;
using SilentOrbit.WebServer;
using SilentOrbit.HttpResponses;
using SilentOrbit.HttpRequests;

namespace ExampleServer
{
	public class ExampleSession : HttpSession
	{
		protected override Response GotRequest (HttpRequest request)
		{
			if (request.Url == "/")
				return new PlainTextResponse ("Hello");
			else
				return new NotFoundResponse ();
		}

	}
}

