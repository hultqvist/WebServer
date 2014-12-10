using System;

namespace SilentOrbit.HttpRequests
{
	public enum HttpMethod
	{
		Unknown = -1,
		GET = 0,
		HEAD = 1,
		POST = 2,
		//PUT = 3,
		//DELETE = 4,
		OPTIONS,
	}

	public static class HttpMethodParser
	{
		public static HttpMethod Parse(string method)
		{
			method = method.ToUpperInvariant();
			switch (method)
			{
				case "GET":
					return HttpMethod.GET;
				case "HEAD":
					return HttpMethod.HEAD;
				case "POST":
					return HttpMethod.POST;
				case "OPTIONS":
					return HttpMethod.OPTIONS;
				default:
					#if DEBUG
					throw new NotImplementedException("Unknown HTTP method: " + method);
					#else
					return HttpMethod.Unknown;
					#endif
			}
		}
	}
}
