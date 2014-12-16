using System;
using System.Text;
using SilentOrbit.WebServer;

namespace SilentOrbit.HttpResponses
{
    public class FileNotFoundResponse : Response
    {
        public string Content { get; set; }

        public FileNotFoundResponse(string filename)
        {
            Content = "Not Found: " + filename;
            Header.StatusCode = System.Net.HttpStatusCode.NotFound;
            Header.ContentType = "text/plain";
        }

        public override byte[] GetBodyBytes()
        {
            return Encoding.ASCII.GetBytes(Content);
        }
    }
}

