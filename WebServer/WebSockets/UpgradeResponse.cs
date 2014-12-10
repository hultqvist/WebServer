using System;
using System.Text;
using System.Collections.Generic;
using SilentOrbit.WebServer;
using SilentOrbit.HttpResponses;
using SilentOrbit.HttpRequests;

namespace SilentOrbit.WebSockets
{
	public class UpgradeResponse : Response
	{
		public WebSocket WebSocket;

		public UpgradeResponse(HttpRequest request, WebSocket socket)
		{
			this.WebSocket = socket;

			//Verify we have a websocket request
			if (request.Upgrade == null || request.Upgrade.ToLowerInvariant() != "websocket")
				throw new NotSupportedException(request.Upgrade);

			Header.ExtraHeaders = new List<string>();

			//Verify version
			if (request.SecWebSocketVersion != "13")
			{
				//Version negotiation
				Header.StatusCode = System.Net.HttpStatusCode.BadRequest;
				Header.ExtraHeaders = new List<string>();
				Header.ExtraHeaders.Add("Sec-WebSocket-Version: 13");
				return;
			}

			Header.StatusCode = System.Net.HttpStatusCode.SwitchingProtocols;

			//Generate reply to Websocket handshake

			//Calculate accept key
			string tohash = request.SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
			var sha = System.Security.Cryptography.SHA1Managed.Create();
			string accept = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(tohash)));

			Header.ExtraHeaders.Add("Upgrade: websocket");
			Header.ExtraHeaders.Add("Connection: Upgrade");
            Header.ExtraHeaders.Add("Sec-WebSocket-Accept: " + accept);
		}

		public override byte[] GetBodyBytes()
		{
			return new byte[0];
		}
	}
}

