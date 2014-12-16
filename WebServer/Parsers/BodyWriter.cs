using System;
using System.IO;

namespace SilentOrbit.Parsers
{
    /// <summary>
    /// Return streams for the SocketParser to write the data into
    /// </summary>
    public abstract class BodyWriter
    {
        /// <summary>
        /// Return a stream into write the next part
        /// </summary>
        public abstract Stream GetBodyStream(MultipartHeaderParser header);

        /// <summary>
        /// Called when all bytes are written into the bodystream
        /// </summary>
        public abstract void BodyComplete();
    }
}

