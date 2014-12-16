using System;
using System.Text;
using System.IO;

#if SUPPORTCHUNKED
namespace SilentOrbit.Connection
{
    public class ChunkedOutput
    {
        bool endOfData = false;
        Stream output;

#region IDataOutput

        public void Send(byte[] buffer, int start, int length)
        {
            if ((length == 0) && (endOfData == false))
                return;

            if (endOfData == false)
            {
                string header = length.ToString("X") + "\r\n";
                byte[] cHeader = Encoding.ASCII.GetBytes(header);
                //output.Send (cHeader, 0, cHeader.Length);
            }
            //output.Send (buffer, start, length);

            //output.Send (new byte[] { 0xd, 0xa }, 0, 2);
        }

        public void EndOfData()
        {
            byte[] cHeader = Encoding.ASCII.GetBytes("0\r\n");
            //output.Send (cHeader, 0, cHeader.Length);

            endOfData = true;
            return;
        }
        #endregion
    }
}

#endif