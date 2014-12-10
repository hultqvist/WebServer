using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Web;
using SilentOrbit.WebServer;
using SilentOrbit.Parsers;

namespace SilentOrbit.HttpRequests
{
	public class HttpRequest
    {
		public HttpMethod Method { get; set; }
        /// <summary>
        /// Url without parameters
        /// </summary>
		public string Url { get; set; }
        /// <summary>
        /// Parameters from the url
        /// </summary>
		public readonly KeyValueStorage Get = new KeyValueStorage();
        /// <summary>
        /// HTTP POST parameters
        /// </summary>
		public readonly KeyValueStorage Post = new KeyValueStorage();
		/// <summary>
		/// Data posted as a JSON
		/// </summary>
		public string JSON { get; set; }
		//Headers

		/// <summary>
		/// POST Body are written to streams given by this class
		/// </summary>
		public BodyWriter Body;

        /// <summary>
        /// Based on HttpVersion in header
        /// Default: 1.0: false, 1.1: true
        /// </summary>
		public bool KeepAlive = false;
		public string Accept;
		public string AcceptCharset;
		public string AcceptEncoding;
		public string AcceptLanguage;
		public string Authorization;
		public long ContentLength;
		public string ContentType;
		public string Boundary;
		public readonly KeyValueStorage Cookies = new KeyValueStorage();
		public string Expect;
		public string From;
		public string Host;
		public string IfMatch;
		public string IfModifiedSince;
		public string IfNoneMatch;
		public string IfRange;
		public string IfUnmodifiedSince;
		public string MaxForwards;
		public string ProxyAuthorization;
		public string Range;
		public string Origin;
		public string Referer;
		public string TE;
		public string UserAgent;
		public string XRealIP;
		//Websockets headers
		public string Upgrade;
//Not explicitly websockets but used by it
		public string SecWebSocketKey;
		public string SecWebSocketProtocol;
		public string SecWebSocketVersion;
		public override string ToString()
		{
			return Method + " " + Url + " " + Get + " " + Post;
		}
    }
}

