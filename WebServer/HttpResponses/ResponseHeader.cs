using System;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace SilentOrbit.HttpResponses
{
    public class ResponseHeader
    {
        //HTTP Headers
        public HttpStatusCode StatusCode { get; set; }

        public bool NoCache { get; set; }

        public string ContentType { get; set; }

        /// <summary>
        /// Http header for redirecting
        /// </summary>
        public string Location { get; set; }

        public readonly CookieCollection Cookies = new CookieCollection();

        public List<string> ExtraHeaders;

        /// <summary>
        /// Indicate if the TCP connection will be closed after this response.
        /// Either because it was a Http/1.0 or that it had the Connection: close header.
        /// Or that the server decided that it should close
        /// </summary>
        public bool Close { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", StatusCode, ContentType, Location);
        }

        public ResponseHeader()
        {
            NoCache = true;
        }

        public void CloseAfterResponse()
        {
            Close = true;
        }

        public byte[] GetHeaderBytes(int bodySize)
        {
            if ((int)StatusCode == 0)
                throw new InvalidProgramException("StatusCode not set");
            if (ContentType == null && bodySize > 0)
                throw new InvalidProgramException("ContentType not set");

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 " + ((int)StatusCode) + " " + StatusCode.ToString() + "\r\n");
            if (Location != null)
                sb.Append("Location: " + Location + "\r\n");
            if (ContentType != null)
            {
                sb.Append("Content-Type: " + ContentType + "\r\n");
                sb.Append("Content-Length: " + bodySize + "\r\n");
                sb.Append("Connection: Keep-Alive\r\n");
            }
            if (NoCache)
            {
                sb.Append("Cache-Control: max-age=0, no-cache, no-store, must-revalidate\r\n");
                sb.Append("Pragma: no-cache\r\n");
            }
            if (Cookies != null)
            {
                AppendCookieHeaders(sb);
            }
            if (ExtraHeaders != null)
            {
                foreach (string e in ExtraHeaders)
                {
                    sb.Append(e);
                    sb.Append("\r\n");
                }
            }
            sb.Append("\r\n");
            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        void AppendCookieHeaders(StringBuilder sb)
        {
            foreach (Cookie c in Cookies)
            {
                sb.Append("Set-Cookie: " + c.Name + "=" + System.Web.HttpUtility.UrlEncode(c.Value));
                sb.Append("; expires=" + c.Expires.ToUniversalTime().ToString("ddd',' dd MMM yyyy HH':'mm':'ss 'GMT'", CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(c.Domain))
                    sb.Append("; domain=" + c.Domain);
                if (!string.IsNullOrWhiteSpace(c.Path))
                    sb.Append("; path=" + c.Path);
                if (c.Secure)
                    sb.Append("; secure");
                if (c.HttpOnly)
                    sb.Append("; HttpOnly");
                sb.Append("\r\n");
            }
        }
    }
}

