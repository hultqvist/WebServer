using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using SilentOrbit.Parsers;
using SilentOrbit.HttpResponses;
using System.Threading.Tasks;
using SilentOrbit.HttpRequests;
using SilentOrbit.WebSockets;

namespace SilentOrbit.WebServer
{
	/// <summary>
	/// Reusable http session
	/// </summary>
	public abstract class HttpSession : IDisposable
	{
		HttpSessionManager manager;
		/// <summary>
		/// Changes for every new connection it is used in
		/// </summary>
		internal WebStream stream;

		public IPEndPoint RemoteEndPoint { get; set; }
		//HTTP
		SocketParser parser;

		protected HttpSession()
		{
		}

		/// <summary>
		/// Start a new thread using the incoming connection
		/// </summary>
		internal void StartAsync(HttpSessionManager m, WebStream s)
		{
			this.manager = m;
			this.stream = s;
			
			this.RemoteEndPoint = s.RemoteEndPoint;
			this.parser = new SocketParser(stream);

			//Known async call without wait
			#pragma warning disable 4014
			RequestsLoop(); //Async, don't wait
			#pragma warning restore 4014
		}

		async Task RequestsLoop()
		{
			try
			{
				while (true)
				{
					request = new HttpRequest();

					//Read Headers
					string headers = await parser.ReadHeaders().ConfigureAwait(false);
					HttpHeaderParser.ParseHeaders(headers, request);

					Response response;

					//Allow handling of request before its body is received
					//this one should also set the bodystream if there is a request body
					try
					{
						response = GotHeaders(request);
					}
					catch (Exception e)
					{
						response = new ExceptionResponse(e, this);
					}
					if (response != null)
					{
						if (response is WebSockets.UpgradeResponse)
						{
							await SendUpgradeResponse(request, (UpgradeResponse)response);
							break;
						}
						
						if (response.Header.Close == false)
							throw new InvalidOperationException("Early response must close connection");
						
						//Ignore sent data pending, send response
						await SendResponse(response).ConfigureAwait(false);
						break;
					}

					if (request.ContentLength > 0)
					{
						await ReadBody().ConfigureAwait(false);
					}

					//Console.WriteLine("GotRequest: " + request.Url);
					try
					{
						response = GotRequest(request);
					}
					catch (Exception e)
					{
						response = new ExceptionResponse(e, this);
					}
					await SendResponse(response).ConfigureAwait(false);

					#if DEBUG
					if (response is WebSockets.UpgradeResponse)
						throw new InvalidOperationException("Websocket upgrade response was expected in GotHeader() response.");
					#endif

					if (request.KeepAlive && !response.Header.Close && stream != null)
						stream.Flush();
					else
						break;
				}
			}
			catch (EndOfStreamException)
			{
				Console.WriteLine("HttpSession: End of stream");
			}
			#if !DEBUG
			catch(Exception ex)
			{
				Console.WriteLine("Exception in HttpSession: " + ex.Message);
			}
			#endif
			finally
			{
				Dispose();
			}
		}

		async Task ReadBody()
		{
			//some built in types
			if (request.Body == null)
			{
				switch (request.ContentType)
				{
					case BodyWriterPost.Mime:
						request.Body = new BodyWriterPost(request);
						break;

					case BodyWriterJson.Mime:
						request.Body = new BodyWriterJson(request);
						break;

					default:
						throw new InvalidOperationException("request has a body but no Body stream was set: " + request.ContentType);
				}
			}

			if (request.ContentType == "multipart/form-data")
				await parser.ReadMultipartBody(request).ConfigureAwait(false);
			else
				await parser.ReadBody(request).ConfigureAwait(false);
		}

		/// <summary>
		/// When complete headers have been received
		/// but no parameters/file transfer is read
		/// if a response is returned, parameters are read and ignored.
		/// The response must close the connection
		/// </summary>
		protected virtual Response GotHeaders(HttpRequest request)
		{
			return null;
		}

		/// <summary>
		/// When headers and post data/file has been received.
		/// Call SendResponse.
		/// </summary>
		protected abstract Response GotRequest(HttpRequest request);

		/// <summary>
		/// Current request being served
		/// </summary>
		HttpRequest request;

		async Task SendUpgradeResponse(HttpRequest request, UpgradeResponse response)
		{
			//Sending headers
			var bodyBytes = response.GetBodyBytes();
			byte[] header = response.Header.GetHeaderBytes(bodyBytes.Length);
			await stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
				
			if (response.Header.StatusCode == HttpStatusCode.SwitchingProtocols)
			{
				manager.UpgradeWebsocket(this, response.WebSocket);
				response.WebSocket.Init(request, stream, manager);
				#pragma warning disable 4014
				response.WebSocket.Start(); //Known async call without wait
				#pragma warning restore 4014
				stream = null; //Prevent the stream from being closed
			}
				
			Dispose();
		}

		/// <summary>
		/// Only used by derived classes for async operation
		/// </summary>
		internal async Task SendResponse(Response response)
		{
			//Debug.WriteLine("SendResponse");

			//Sending headers
			var bodyBytes = response.GetBodyBytes();
			int length = 0;
			if (bodyBytes != null)
				length = bodyBytes.Length;

			byte[] header = response.Header.GetHeaderBytes(length);
			await stream.WriteAsync(header, 0, header.Length).ConfigureAwait(false);
			//Debug.WriteLine("SendHeaderCompleted: " + e.SocketError);

			if (request.Method != HttpMethod.HEAD)
			{
				//Sending Body
				if (bodyBytes != null)
					await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
			}
			else if (bodyBytes != null)
				throw new InvalidOperationException("HEAD response should not have a body set");
		}

		void Error(string message)
		{
			Console.WriteLine("Error: " + message);
			Dispose();
		}

		public virtual void Dispose()
		{
			manager.SessionClosed(this);

			//Don't close socket if we are moving to a Websocket
			if (stream == null)
				return;

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
		}
	}
}
