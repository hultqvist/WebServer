using System;
using SilentOrbit.WebServer;
using System.Net;
using System.IO;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.Parsers
{
    /// <summary>
    /// Parse HTTP header strings into a HttpRequest object
    /// </summary>
    public static class HttpHeaderParser
    {
        public static void ParseHeaders(string headers, HttpRequest request)
        {
            //Debug.WriteLine("ProcessHeaders: " + headers.Length);
            string[] h = headers.Trim('\n').Split('\n');
            if (h.Length < 1)
                throw new ArgumentException("Too few lines in header");

            //Debug.WriteLine("Parsing headers");

            //First line
            string[] parts = h[0].Split(' ');
            if (parts.Length != 3)
                throw new HeaderException("Invalid header: " + h[0], HttpStatusCode.BadRequest);

            //Method
            request.Method = HttpMethodParser.Parse(parts[0]);

            //Url and Get parameters
            {
                int pos = parts[1].IndexOf('?');
                if (pos < 0)
                    request.Url = parts[1];
                else
                {
                    request.Url = parts[1].Substring(0, pos);
                    PostParser.ParseParameters(request.Get, parts[1].Substring(pos + 1));
                }
            }
            //HTTP Version, keep alive defaults
            if (parts[2] == "HTTP/1.0")
                request.KeepAlive = false;
            else if (parts[2] == "HTTP/1.1")
                request.KeepAlive = true;

            //Remaining headers
            for (int n = 1; n < h.Length; n++)
            {
                if (h[n].Length == 0)
                    throw new InvalidDataException("Unexpected empty line inside headers");
                if (h[n][0] == ' ' || h[n][0] == '\t')
                    throw new NotSupportedException("Header folding");
                int keysep = h[n].IndexOf(':');
                if (keysep < 0)
                    continue;

                string key = h[n].Substring(0, keysep).ToLowerInvariant();
                string val = h[n].Substring(keysep + 1).Trim();
                ParseHeader(request, key, val);
            }
            //Debug.WriteLine("HeaderParsing Done");
        }

        static void ParseHeader(HttpRequest req, string key, string value)
        {
            switch (key)
            {
                case "accept":
                    req.Accept = value;
                    break;
                case "accept-charset":
                    req.AcceptCharset = value;
                    break;
                case "accept-encoding":
                    req.AcceptEncoding = value;
                    break;
                case "accept-language":
                    req.AcceptLanguage = value;
                    break;
                case "authorization":
                    req.Authorization = value;
                    break;
                case "content-length":
                    long.TryParse(value, out req.ContentLength);
                    break;
                case "content-type":
                    int sep = value.IndexOf(';');
                    if (sep > 0)
                    {
                        req.ContentType = value.Substring(0, sep).ToLowerInvariant();
                        const string boundaryMarker = "boundary=";
                        sep = value.IndexOf(boundaryMarker, sep, StringComparison.Ordinal);
                        if (sep > 0)
                            req.Boundary = value.Substring(sep + boundaryMarker.Length);
                    }
                    else
                        req.ContentType = value.ToLowerInvariant();
                    break;
                case "connection":
                    value = value.ToLowerInvariant();
                    if (value == "keep-alive")
                        req.KeepAlive = true;
                    if (value == "close")
                        req.KeepAlive = false;
                    break;
                case "cookie":
                    {
                        string[] cs = value.Split(';');
                        foreach (string c in cs)
                        {
                            int eqSep = c.IndexOf('=');
                            if (eqSep < 0)
                            {
                                Debug.WriteLine("Invalid Cookie: " + c + " in " + value);
                                continue;
                            }
                            string cKey = c.Substring(0, eqSep).Trim();
                            string cVal = System.Web.HttpUtility.UrlDecode(c.Substring(eqSep + 1).Trim());
                            req.Cookies[cKey] = cVal;
                        }
                    }
                    break;
                case "expect":
                    req.Expect = value;
                    break;
                case "from":
                    req.From = value;
                    break;
                case "host":
                    req.Host = value;
                    break;
                case "if-match":
                    req.IfMatch = value;
                    break;
                case "if-modified-since":
                    req.IfModifiedSince = value;
                    break;
                case "if-none-match":
                    req.IfNoneMatch = value;
                    break;
                case "if-range":
                    req.IfRange = value;
                    break;
                case "if-unmodified-since":
                    req.IfUnmodifiedSince = value;
                    break;
                case "max-forwards":
                    req.MaxForwards = value;
                    break;
                case "origin":
                    req.Origin = value;
                    break;
                case "proxy-authorization":
                    req.ProxyAuthorization = value;
                    break;
                case "range":
                    req.Range = value;
                    break;
                case "referer":
                    req.Referer = value;
                    break;
                case "te":
                    req.TE = value;
                    break;
                case "upgrade":
                    req.Upgrade = value;
                    break;
                case "user-agent":
                    req.UserAgent = value;
                    break;
                case "x-real-ip":
                    req.XRealIP = value;
                    break;

                //Websockets
                case "sec-websocket-key":
                    req.SecWebSocketKey = value;
                    break;
                case "sec-websocket-protocol":
                    req.SecWebSocketProtocol = value;
                    break;
                case "sec-websocket-version":
                    req.SecWebSocketVersion = value;
                    break;
                case "sec-websocket-extensions":
                    break;

                //Ignore known unhandled
                case "pragma":
                case "cache-control":
                case "x-requested-with":
                case "dnt": //do not track
                case "access-control-request-method":
                case "access-control-request-headers":
                    break;
#if DEBUG
                default:
                    Console.WriteLine("Unhandled header: " + key + ": " + value);
                    break;
#endif
            }
        }
    }
}

