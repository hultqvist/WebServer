using System;
using System.Text;
using System.Net.Sockets;
using SilentOrbit.WebServer;
using SilentOrbit.HttpResponses;
using System.Threading.Tasks;
using System.IO;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.WebSockets
{
    public abstract class WebSocket : IDisposable
    {
        HttpSessionManager manager;
        /// <summary>
        /// Initial HTTP request of the websocket session
        /// </summary>
        internal protected HttpRequest request;
        WebStream stream;
        readonly byte[] buffer = new byte[50000];
        /// <summary>
        /// Position to start reading,
        /// bytes received
        /// </summary>
        int offset = 0;
        /// <summary>
        /// Set to false when closing the connection server side
        /// </summary>
        bool active = true;

        public void Init(HttpRequest request, WebStream stream, HttpSessionManager manager)
        {
            this.request = request;
            this.stream = stream;
            this.manager = manager;

            stream.NoDelay = true;
        }

        public void Close(CloseReason code, string reason)
        {
            active = false;
            try
            {
                SendClose(code, reason);
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine("While closing: " + ode.Message);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("While closing: " + ioe.Message);
            }
        }

        public virtual void Dispose()
        {
            //Debug.WriteLine("Dispose");

            try
            {
                stream.Shutdown(SocketShutdown.Send);
            }
            catch (SocketException)
            {
            }
            try
            {
                stream.LingerState.Enabled = false;
                stream.Close();
            }
            catch (Exception)
            {
            }
            try
            {
                stream.Close();
            }
            catch (Exception)
            {
            }

            manager.SessionClosed(this);
        }

        /// <summary>
        /// Called when the websocket handshake is complete.
        /// </summary>
        protected abstract void Connected();

        public async Task Start()
        {
            try
            {
                Connected();

                while (true)
                {
                    if (ParseBuffer() == false)
                        return;

                    if (active == false)
                        return;

                    //Fill buffer
                    int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset).ConfigureAwait(false);
                    if (read == 0)
                        return;
                    offset += read;
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (EndOfStreamException)
            {
            }
            catch (IOException)
            {
            }
#if !DEBUG
            catch (Exception e)
            {
                Exception(e);
            } 
#endif
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Exception while parsing
        /// </summary>
        /// <param name="e">E.</param>
        protected virtual void Exception(Exception e)
        {
        }

        /// <summary>
        /// Parse any full message in the buffer and send it to the handler
        /// Return true to continue, false to close
        /// </summary>
        bool ParseBuffer()
        {
            while (true)
            {
                if (offset < 2)
                    return true; //Need more

                //Debug.WriteLine(BitConverter.ToString(buffer, 0, offset));

                bool fin = (buffer[0] & 0x80) == 0x80;
                Opcode op = (Opcode)(buffer[0] & 0xF);
                bool mask = (buffer[1] & 0x80) == 0x80;
                int length = buffer[1] & 0x7F;

                int header = 2;
                if (mask)
                    header += 4;

                if (length == 126)
                {
                    header += 2;

                    if (offset < 4)
                        return true; //Need more

                    //Next 2 bytes is length
                    length = (buffer[2] << 8) + buffer[3];
                }
                else if (length == 127)
                {
                    header += 8;

                    if (offset < 10)
                        return true; //Need more

                    //Next 8 bytes is length
                    throw new NotSupportedException();
                }

                int frameSize = header + length;

                if (frameSize > buffer.Length)
                {
                    Close(CloseReason.MessageTooBig, "");
                    return false;
                }

                if (frameSize > offset)
                    return true; //Need more
                //Complete message loaded

                if (fin == false)
                {
#if DEBUG
                    throw new NotImplementedException("Multiframe message not implemented");
#else
                    Close(CloseReason.PolicyViolation, "Multiframe message not implemented");
                    return false;
#endif
                }

                //Apply mask
                int maskStart = header - 4;
                for (int n = 0; n < length; n++)
                {
                    buffer[header + n] ^= buffer[maskStart + (n % 4)];
                }

                //Received frame
                switch (op)
                {
                    case Opcode.Text:
                        string text = Encoding.UTF8.GetString(buffer, header, length);
                        ReceivedText(text);
                        break;

                    case Opcode.Binary:
                        byte[] data = new byte[length];
                        Buffer.BlockCopy(buffer, header, data, 0, length);
                        ReceivedData(data);
                        break;

                    case Opcode.Close:
                        return false;

                    case Opcode.Continuation:
                        throw new NotSupportedException(op.ToString());

                    case Opcode.Ping:
                        Close(CloseReason.ProtocolError, "Hey! ping is my thing");
                        return false;

                    case Opcode.Pong://Response to ping
                        //May also be sent unsolicited, if so it is ignored
                        string pong = Encoding.UTF8.GetString(buffer, header, length);
                        Console.WriteLine("Ignoring PONG: " + pong);
                        break;

                    default:
                        Close(CloseReason.ProtocolError, "Hey! ping is my thing");
#if DEBUG
                        throw new NotImplementedException(op.ToString());
#else
                        return false;
#endif
                }

                //Move buffer
                if (frameSize < offset)
                {
                    Buffer.BlockCopy(buffer, frameSize, buffer, 0, offset - frameSize);
                    offset = offset - frameSize;
                }
                else
                    offset = 0;
            }
        }

        /// <summary>
        /// Bit 7 indicated that this frame is the last one in current message
        /// </summary>
        const byte finalFrame = 0x80;
        const byte length16bit = 126;
        const byte length64bit = 127;

        public async Task SendText(string text)
        {
            if (active == false)
                return;

            long length = Encoding.UTF8.GetByteCount(text);
            byte[] sendBuffer;
            if (length < 126)
            {
                sendBuffer = new byte[2 + length];
                sendBuffer[0] = finalFrame | (byte)Opcode.Text;
                sendBuffer[1] = (byte)length;
                Encoding.UTF8.GetBytes(text, 0, text.Length, sendBuffer, 2);
            }
            else if (length < 256 * 256)
            {
                sendBuffer = new byte[4 + length];
                sendBuffer[0] = finalFrame | (byte)Opcode.Text;
                sendBuffer[1] = length16bit;
                sendBuffer[2] = (byte)(length >> 8);
                sendBuffer[3] = (byte)(length & 0xFF);
                Encoding.UTF8.GetBytes(text, 0, text.Length, sendBuffer, 4);
            }
            else
            {
                sendBuffer = new byte[10 + length];
                sendBuffer[0] = finalFrame | (byte)Opcode.Text;
                sendBuffer[1] = length64bit;
                sendBuffer[2] = (byte)((length >> 56) & 0xFF);
                sendBuffer[3] = (byte)((length >> 48) & 0xFF);
                sendBuffer[4] = (byte)((length >> 40) & 0xFF);
                sendBuffer[5] = (byte)((length >> 32) & 0xFF);
                sendBuffer[6] = (byte)((length >> 24) & 0xFF);
                sendBuffer[7] = (byte)((length >> 16) & 0xFF);
                sendBuffer[8] = (byte)((length >> 8) & 0xFF);
                sendBuffer[9] = (byte)(length & 0xFF);
                Encoding.UTF8.GetBytes(text, 0, text.Length, sendBuffer, 10);
            }
            await stream.WriteAsync(sendBuffer, 0, sendBuffer.Length).ConfigureAwait(false);
        }

        void SendClose(CloseReason code, string reason)
        {
            //2 first bytes are the status code
            long length = Encoding.UTF8.GetByteCount(reason) + 2;
            byte[] sendBuffer;
            if (length >= 126 - 2) //
                throw new NotSupportedException("Only implemented close reason messages up to 124 bytes");
            sendBuffer = new byte[2 + 2 + length];
            sendBuffer[0] = finalFrame | (byte)Opcode.Close;
            sendBuffer[1] = (byte)length;
            sendBuffer[2] = (byte)(((int)code) >> 8);
            sendBuffer[3] = (byte)(((int)code) & 0xFF);
            Encoding.UTF8.GetBytes(reason, 0, reason.Length, sendBuffer, 4);
            stream.WriteAsync(sendBuffer, 0, sendBuffer.Length).Wait();
        }

        protected abstract void ReceivedText(string text);

        protected abstract void ReceivedData(byte[] data);
    }
}

