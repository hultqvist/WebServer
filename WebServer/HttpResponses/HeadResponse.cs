using System;

namespace SilentOrbit.HttpResponses
{
	public class HeadResponse : Response
	{
		public HeadResponse()
		{
			Header.StatusCode = System.Net.HttpStatusCode.OK;
		}

		public HeadResponse(System.Net.HttpStatusCode status)
		{
			Header.StatusCode = status;
		}

		public override byte[] GetBodyBytes()
		{
			return null;
		}
	}
}

