using System;
using System.Net;
using System.Text;
using System.IO;
using SilentOrbit.WebServer;

#if SUPPORTCHUNKED
namespace SilentOrbit.Connection
{
    public class ChunkedInput
    {

        /// <summary>
        /// Read headers in a chunked encoding
        /// Return a string with the chunk header
        /// </summary>
        string ReadChunkedHeader(Stream input)
        {
            byte[] header = new byte[30];
            int index = 0;
            while (true)
            {
                if (index >= header.Length)
                    throw new HeaderException("Chunked header is too large", HttpStatusCode.BadGateway);

                //Read one byte
                //input.Receive (header, index, 1);

                //Skip leading space
                if (index == 0)
                {
                    if (header[index] == 0x20)
                        continue;
                }

                index++;

                if (index > 2 && header[index - 1] == 0xa)
                {
                    return Encoding.ASCII.GetString(header, 0, index);
                }
            }
        }

    }
}

#endif