using System;

namespace SilentOrbit.HttpResponses
{
    public class NullResponse : Response
    {
        static readonly byte[] bytes = new byte[0];
        
        public override byte[] GetBodyBytes()
        {
            return bytes;
        }
    }
}

