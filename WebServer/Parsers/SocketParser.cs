using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using SilentOrbit.Parsers;
using SilentOrbit.WebServer;
using SilentOrbit.HttpResponses;
using System.Threading.Tasks;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.Parsers
{
    public class SocketParser
    {
        readonly WebStream stream;
        //TODO: test the 95% percentile header size and use it
        /// <summary>
        /// Read data to header where we might store remaining data if we read too much
        /// </summary>
        readonly byte[] buffer = new byte[1024 * 4];
        /// <summary>
        /// Bytes read into buffer
        /// </summary>
        int bufferPointer = 0;
        /// <summary>
        /// Count the CR and NL in a row to determine end of header
        /// </summary>
        int countCR;

        public SocketParser(WebStream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Read text until first empty line is reached
        /// </summary>
        /// <param name="completed">Completed.</param>
        public async Task<string> ReadHeaders()
        {
            //Console.WriteLine("ReadHeaders: " + stream.RemoteEndPoint);
            this.countCR = 0;

            //Read entire header in one go
            while (true)
            {
                //Start by scanning data already in the buffer
                int next = ScanForEmptyLine();

                //Entire buffer is part of header, we need more
                if (next == -1)
                {
                    if (bufferPointer == buffer.Length)
                        throw new NotSupportedException("Too large header: max size currently is " + buffer.Length);

                    //Read more
                    //Console.WriteLine("ReadHeaders() FillBuffer()");
                    await FillBuffer().ConfigureAwait(false);
                    //Console.WriteLine("ReadHeaders, Read: " + stream.RemoteEndPoint + "\t" + bufferPointer);
                    continue;
                }

                //Console.WriteLine("ReadHeaders, Done: " + stream.RemoteEndPoint + "\t" + bufferPointer);

                //We got full headers

                string headers = Encoding.UTF8.GetString(buffer, 0, next).Replace("\r\n", "\n").ToString();
                NextBuffer(next);
                return headers;
            }
        }

        /// <summary>
        /// Scan buffer for an empty line.
        /// If an empty line is found return the position of the next character.
        /// Else return -1 if entire buffer is within the headers.
        /// </summary>
        int ScanForEmptyLine()
        {
            for (int n = 0; n < bufferPointer; n++)
            {
                switch (countCR)
                {
                    case 0:
                        if (buffer[n] == '\r')
                            countCR = 1;
                        else if (buffer[n] == '\n')
                        {
                            countCR = 2;
                            //Console.WriteLine("Warning: Line end starting with NL");
                        }
                        else
                            countCR = 0;
                        continue;

                    case 1: //already got CR
                        if (buffer[n] == '\n')
                            countCR = 2;
                        else
                            countCR = 0; //Console.WriteLine("Warning: no NL after CR");
                        continue;

                    case 2: //already got CR NL, or NL only
                        if (buffer[n] == '\r')
                        {
                            countCR = 3;
                            continue;
                        }
                        if (buffer[n] == '\n')
                            return n + 1;//We got full headers
                        countCR = 0; //regular new line, still header
                        continue;

                    case 3: //already got CR-NL-CR
                        if (buffer[n] == '\n')
                            return n + 1; //we got full header
                        countCR = 0; //Console.WriteLine("Warning: Expected NL after CR-NL-CR");
                        continue;

                    default:
                        throw new InvalidOperationException("Unexpected countCR: " + countCR);
                }
            }
            return -1;
        }

        /// <summary>
        /// Read the length delimited post body
        /// </summary>
        public async Task ReadBody(HttpRequest request)
        {
            long bodyRead = 0;

            using (Stream bodyStream = request.Body.GetBodyStream(MultipartHeaderParser.ByContentType(request.ContentType)))
            {
                //Write previously read data
                int toWrite = bufferPointer;
                if (toWrite > 0)
                {
                    if (toWrite > request.ContentLength)
                        toWrite = (int)request.ContentLength;
                    bodyStream.Write(buffer, 0, toWrite);
                    bodyRead += toWrite;
                    //Console.WriteLine("BODY READ now " + toWrite + " total " + bodyRead + "/" + request.ContentLength);

                    //Write remaining bytes to beginning of buffer
                    NextBuffer(toWrite);
                }

                //Read more
                //TODO: two buffers swapping and reading/writing them in parallel
                while (bodyRead < request.ContentLength)
                {
                    long left = request.ContentLength - bodyRead;
                    int toRead;
                    if (left > buffer.Length)
                        toRead = buffer.Length;
                    else
                        toRead = (int)left;
                    await FillBuffer(toRead).ConfigureAwait(false);

                    if (bufferPointer < toRead)
                        toRead = bufferPointer;

                    //Console.WriteLine("ReadHeaderAsyncCompleted: " + e.BytesTransferred);
                    bodyStream.Write(buffer, 0, toRead);
                    bodyRead += toRead;
                    //Console.WriteLine("BODY READ now " + toRead + " total " + bodyRead + "/" + request.ContentLength);
                    NextBuffer(bufferPointer);
                }

                //Done reading
                request.Body.BodyComplete();
            }
        }

        /// <summary>
        /// multipart/form-data, file uploads http://tools.ietf.org/html/rfc2388
        /// </summary>
        public async Task ReadMultipartBody(HttpRequest request)
        {
            if (request.Boundary == null)
                throw new Exception("Missing boundary: " + request.ContentType);

            //Console.WriteLine("Total Size: " + request.ContentLength);
            //Console.WriteLine("Multipart: " + request.Boundary);

            if (await ReadToBoundary(request.Boundary, null).ConfigureAwait(false))
                return;

            while (true)
            {
                string headers = await ReadHeaders().ConfigureAwait(false);
                var mhp = new MultipartHeaderParser(headers);

                bool last;
                using (var bodyStream = request.Body.GetBodyStream(mhp))
                {
                    last = await ReadToBoundary(request.Boundary, bodyStream).ConfigureAwait(false);
                }
                request.Body.BodyComplete();
                if (last)
                    break;
            }
        }

        /// <summary>
        /// Read all data to the next boundary and write it to Stream target.
        /// Return true if it is the final boundary.
        /// </summary>
        /// <returns><c>true</c>, if to last boundary was read.</returns>
        /// <param name="boundary">multipart boundary string</param>
        /// <param name="target">Target stream for data, if null data is discarded</param>
        async Task<bool> ReadToBoundary(string boundary, Stream target)
        {
            //Read past boundary and the following CRNL
            byte[] bound = Encoding.ASCII.GetBytes("\r\n--" + boundary);
            //Start by skipping first two bytes, "\r\n" in case the boundary starts at a new line right ahead
            int boundIndex = 2; //Index matching towards bound
            int bufferBoundaryStart = -1;

            //Console.WriteLine("RTB boundary: " + Encoding.ASCII.GetString(bound));

            while (true)
            {
                //Console.WriteLine("RTB: Buffer: " + Encoding.ASCII.GetString(buffer, 0, bufferPointer));
                //Parse existing buffer
                for (int n = 0; n < bufferPointer; n++)
                {
                    if (buffer[n] == bound[boundIndex])
                    {
                        //Match
                        if (boundIndex == 0 || bufferBoundaryStart == -1)
                            bufferBoundaryStart = n;
                        boundIndex++;

                        if (boundIndex == bound.Length) //Last
                        {
                            //Pass data before boundary
                            //Console.WriteLine("RTB: Passed: " + Encoding.ASCII.GetString(buffer, 0, bufferBoundaryStart));
                            if (target != null)
                            {
                                if (bufferBoundaryStart > 0)
                                    target.Write(buffer, 0, bufferBoundaryStart);
                            }

                            //Found complete boundary
                            NextBuffer(n + 1);
                            goto boundaryFound;
                        }
                    }
                    else
                    {
                        boundIndex = 0;
                    }
                }

                //Pass up to matching buffer
                //Console.WriteLine("RTB: Passed: " + Encoding.ASCII.GetString(buffer, 0, bufferPointer - boundIndex));
                int toWrite = bufferPointer - boundIndex;
                if (target != null)
                {
                    if (toWrite > 0)
                        target.Write(buffer, 0, toWrite);
                }
                NextBuffer(toWrite);

                //Read more
                //Console.WriteLine("RTB: Need more...");
                await FillBuffer().ConfigureAwait(false);
                //Console.WriteLine(bufferPointer);
            }

        boundaryFound:
            //Console.WriteLine("RTB: Complete boundary found");
            //Determine ending
            await FillBuffer(2).ConfigureAwait(false);
            if (buffer[0] == '-' && buffer[1] == '-')
            {
                await FillBuffer(4).ConfigureAwait(false);
                if (buffer[2] != '\r' || buffer[3] != '\n')
                    throw new FormatException("Expected --\\r\\n after boundary, but got: " + Encoding.ASCII.GetString(buffer, 0, 4));
                NextBuffer(4); // "--\r\n"
                return true; //Last boundary
            }

            //Save remaining to the future buffer
            if (buffer[0] != '\r' || buffer[1] != '\n')
                throw new FormatException("Expected \\r\\n after boundary, but got: " + Encoding.ASCII.GetString(buffer, 0, 2));
            NextBuffer(2); // "\r\n"
            return false; //Not last boundary
        }

        /// <summary>
        /// Fill buffer up to specified bytes
        /// </summary>
        async Task FillBuffer(int bytesNeeded)
        {
            //Console.Write("FillBuffer(" + bytesNeeded + ")");
            if (bytesNeeded > buffer.Length)
                bytesNeeded = buffer.Length;
            while (bufferPointer < bytesNeeded)
            {
                int read = await stream.ReadAsync(buffer, bufferPointer, bytesNeeded - bufferPointer).ConfigureAwait(false);
                if (read <= 0)
                    throw new EndOfStreamException();
                bufferPointer += read;
            }
            //Console.WriteLine(" got " + bufferPointer);
        }

        /// <summary>
        /// Fill buffer with whatever available
        /// </summary>
        async Task FillBuffer()
        {
            //Console.Write("FillBuffer() from " + (bufferPointer) + " stream " + stream);
            int read = await stream.ReadAsync(buffer, bufferPointer, buffer.Length - bufferPointer).ConfigureAwait(false);
            //Console.WriteLine(" got " + read);
            if (read <= 0)
                throw new EndOfStreamException();
            bufferPointer += read;
        }

        /// <summary>
        /// Copy remaining bytes in buffer starting at pointerNext to the beginning of the buffer
        /// </summary>
        /// <param name="pointerNext">Start of remaining data.</param>
        void NextBuffer(int pointerRemaining)
        {
            //Console.WriteLine("NextBuffer(" + pointerRemaining + ") remaining " + (bufferPointer - pointerRemaining));

            if (pointerRemaining > bufferPointer || pointerRemaining < 0)
                throw new InvalidOperationException(pointerRemaining + " out of " + bufferPointer);
            if (pointerRemaining == bufferPointer)
            {
                bufferPointer = 0;
                return;
            }
            bufferPointer -= pointerRemaining;
            Buffer.BlockCopy(buffer, pointerRemaining, buffer, 0, bufferPointer);
            //Safe even though overlapping:
            //http://msdn.microsoft.com/en-us/library/system.buffer.blockcopy.aspx
        }
    }
}

