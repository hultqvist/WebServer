using System;
using SilentOrbit.WebServer;
using System.Net;
using System.Text;

namespace SilentOrbit.HttpResponses
{
	public class NotFoundResponse : Response
	{
		public NotFoundResponse()
		{
			Header.StatusCode = HttpStatusCode.NotFound;
			Header.ContentType = "text/plain";
		}

		static byte[] notfound = Encoding.ASCII.GetBytes("Not Found");

		public override byte[] GetBodyBytes()
		{
			return notfound;
		}
	}
}

