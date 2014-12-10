using System;

namespace SilentOrbit.HttpResponses
{
	public class NullResponse : Response
	{
		static byte[] bytes = new byte[0];

		public NullResponse()
		{
			
		}

		public override byte[] GetBodyBytes()
		{
			return bytes;
		}

	}
}

