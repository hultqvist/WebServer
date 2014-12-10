using System;
using SilentOrbit.WebServer;

namespace SilentOrbit.HttpResponses
{
	public class RedirectResponse : Response
	{
		public RedirectResponse(string url)
		{
			Header.StatusCode = System.Net.HttpStatusCode.MovedPermanently;
			Header.Location = url;
		}

		public RedirectResponse(string url, System.Net.HttpStatusCode status)
		{
			Header.StatusCode = status;
			Header.Location = url;
		}

		static byte[] none = new byte[0];

		public override byte[] GetBodyBytes()
		{
			return none;
		}
	}
}

