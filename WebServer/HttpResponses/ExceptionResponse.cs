using System;
using System.Text;
using System.Net;
using SilentOrbit.WebServer;

namespace SilentOrbit.HttpResponses
{
	public class ExceptionResponse : Response
	{
		readonly Exception ex;
		#if DEBUG
		readonly HttpSession session;
		#endif
		public ExceptionResponse(Exception e, HttpSession s)
		{
			this.ex = e;
#if DEBUG
			this.session = s;
#endif
			Header.StatusCode = System.Net.HttpStatusCode.InternalServerError;
			Header.ContentType = "text/html; charset=UTF-8"; 
			Header.CloseAfterResponse();
		}

		public override byte[] GetBodyBytes()
		{
			return Encoding.UTF8.GetBytes(@"<!DOCTYPE html>
<html>
<head>
    <title>Internal Server Error</title>
</head>
<body>
" + HtmlFormat(ex) + @"
</body>
</html>");
			;
		}

		string HtmlFormat(Exception e)
		{
			string s =
				"<h1>" + Escape(e.GetType().Name.Replace("Exception", "")) + "</h1>" +
				"<p>" + Escape(e.Message) + @"</p>";
#if DEBUG
			var ip = session.stream.RemoteEndPoint;
			if (ip != null && IPAddress.IsLoopback(ip.Address))
				s += "<pre>" + Escape(e.StackTrace) + "</pre>";
#endif
			if (e.InnerException != null)
				s += HtmlFormat(e.InnerException);
			return s;
		}
	}
}

