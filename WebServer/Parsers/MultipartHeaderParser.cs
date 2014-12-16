using System;

namespace SilentOrbit.Parsers
{
    /// <summary>
    /// Parses only the specific headers in multipart http headers
    /// </summary>
    public class MultipartHeaderParser
    {
        public string Name { get; private set; }

        public string Filename { get; set; }

        public string ContentType { get; set; }

        public static MultipartHeaderParser ByContentType(string contentType)
        {
            var mhp = new MultipartHeaderParser();
            mhp.ContentType = contentType;
            return mhp;
        }

        public override string ToString()
        {
            return string.Format("[MultipartHeaderParser: Name={0}, Filename={1}, ContentType={2}]", Name, Filename, ContentType);
        }

        MultipartHeaderParser()
        {

        }

        public MultipartHeaderParser(string headers)
        {
            //headers:
            //Content-Disposition: form-data; name="file"; filename="Windows"
            //Content-Type: application/octet-stream
            //

            string[] lines = headers.Split('\n');
            foreach (string line in lines)
            {
                int sep = line.IndexOf(':');
                if (sep <= 0)
                    continue;
                string key = line.Substring(0, sep).Trim().ToLowerInvariant();
                string val = line.Substring(sep + 1).Trim(' ', '\r', '\n');

                switch (key)
                {
                    case "content-disposition":
                        ParseContentDisposition(val);
                        break;

                    case "content-type": //application/octet-stream
                        ContentType = val.ToLowerInvariant();
                        break;

                    default:
#if DEBUG
                        throw new NotImplementedException(key + ": " + val);
#else
						break;
#endif
                }
            }
        }

        void ParseContentDisposition(string value)
        {
            //form-data; name="file"; filename="Windows"
            var parts = value.Split(';');
            if (parts[0] != "form-data")
                throw new NotImplementedException();
            for (int n = 1; n < parts.Length; n++)
            {
                int eqPos = parts[n].IndexOf('=');
                if (eqPos <= 0)
                    continue; //missing "="
                string key = parts[n].Substring(0, eqPos).Trim();
                string val = parts[n].Substring(eqPos + 1).Trim();
                switch (key)
                {
                    case "name":
                        Name = val.Trim(' ', '"');
                        break;
                    case "filename":
                        Filename = val.Trim(' ', '"');
                        break;
                    default:
                        throw new NotImplementedException(key);
                }
            }
        }
    }
}

