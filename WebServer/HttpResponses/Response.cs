using System;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Web;
using System.Collections.Generic;

namespace SilentOrbit.HttpResponses
{
    public abstract class Response
    {
        public readonly ResponseHeader Header = new ResponseHeader();

        //TODO: chunked and async delivery
        public abstract byte[] GetBodyBytes();

        #region Helpers

        public static string Escape(string text)
        {
            return HttpUtility.HtmlEncode(text);
        }

        public static string EscapeUrl(string text)
        {
            return HttpUtility.UrlEncode(text);
        }

        #endregion

        public override string ToString()
        {
            return Header.ToString();
        }
    }
}
