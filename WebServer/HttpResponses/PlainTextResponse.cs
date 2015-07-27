using System;
using SilentOrbit.WebServer;
using System.Text;

namespace SilentOrbit.HttpResponses
{
    public class PlainTextResponse : Response
    {
        readonly string text;

        public PlainTextResponse(string text)
        {
            this.text = text;
            Header.StatusCode = System.Net.HttpStatusCode.OK;
            Header.ContentType = "text/plain; charset=UTF-8";
        }

        public override byte[] GetBodyBytes()
        {
            return Encoding.UTF8.GetBytes(text);
        }
    }
}

