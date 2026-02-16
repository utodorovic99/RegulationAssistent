using APIGatewayService.Context.Common;
using System.Net;

namespace APIGatewayService.Common
{
	internal sealed class HttpProcessObject : IProcessingObject
	{
		private readonly HttpListenerContext context;

		public HttpProcessObject(HttpListenerContext context)
		{
			this.context = context;
		}

		public HttpListenerRequest Request => context.Request;
		public HttpListenerResponse Response => context.Response;
	}
}
