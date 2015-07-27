using System;
using System.IO;
using SilentOrbit.Parsers;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.Parsers
{
    /// <summary>
    /// The usual POST form fields in teh same format as the GET query
    /// </summary>
    public class BodyWriterPost : BodyWriter
    {
        public const string Mime = "application/x-www-form-urlencoded";

        MemoryStream stream;
        readonly HttpRequest request;

        public BodyWriterPost(HttpRequest request)
        {
            this.request = request;
        }

        public override Stream GetBodyStream(MultipartHeaderParser header)
        {
            if (stream != null)
                throw new InvalidOperationException("POST can only have one body stream");
            stream = new MemoryStream();
            return stream;
        }

        public override void BodyComplete()
        {
            PostParser.ParsePost(stream.ToArray(), request);
        }
    }
}

