using System;
using System.Text;
using SilentOrbit.WebServer;

namespace SilentOrbit.HttpResponses
{
    public class BadRequestResponse : Response
    {
		public string Content { get; set; }

        public BadRequestResponse()
        {
			Content = "No";
            Header.StatusCode = System.Net.HttpStatusCode.BadRequest;
            Header.ContentType = "text/plain"; 
        }

		public BadRequestResponse(string content) : this()
		{
			this.Content = content;
			this.Header.CloseAfterResponse();
		}

        public override byte[] GetBodyBytes()
        {
            return Encoding.ASCII.GetBytes(Content);
        }
    }
}

