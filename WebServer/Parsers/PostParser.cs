using System;
using SilentOrbit.WebServer;
using System.IO;
using System.Text;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.Parsers
{
    public static class PostParser
    {
        /// <summary>
        /// Parse POST and GET data parameter string
        /// </summary>
        /// <param name='storage'>
        /// Where to put the data
        /// </param>
        /// <param name='encoded'>
        /// encoded parameter string
        /// </param>
        public static void ParseParameters(KeyValueStorage storage, string encoded)
        {
            string[] args = encoded.Split('&');
            foreach (string a in args)
            {
                if (a == "")
                    continue;
                int eqPos = a.IndexOf('=');
                if (eqPos < 0)
                    throw new InvalidDataException("Missing = in POST/GET parameter");
                string key = a.Substring(0, eqPos);
                string val = System.Web.HttpUtility.UrlDecode(a.Substring(eqPos + 1));

                storage[key] = val;
            }
        }


        /// <summary>
        /// Parse HTTP POST parameters
        /// </summary>
        public static void ParsePost(byte[] buffer, HttpRequest request)
        {
            string s = Encoding.ASCII.GetString(buffer, 0, (int)request.ContentLength);
            ParseParameters(request.Post, s);
        }
    }
}

