using System;
using System.IO;
using System.Text;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.Parsers
{
    /// <summary>
    /// Single body posted as json data
    /// </summary>
    public class BodyWriterJson : BodyWriter
    {
        public const string Mime = "application/json";

        MemoryStream stream;
        readonly HttpRequest request;

        public BodyWriterJson(HttpRequest request)
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
            var bytes = stream.ToArray();
            request.JSON = Encoding.UTF8.GetString(bytes);
        }
    }
}

