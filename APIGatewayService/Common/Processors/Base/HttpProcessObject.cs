using System.Net;
using APIGatewayService.Context.Common;

namespace APIGatewayService.Common
{
	/// <summary>
	/// Http process object that encapsulates the HTTP request and response context for processing by request processors.
	/// </summary>
	internal sealed class HttpProcessObject : IProcessingObject
	{
		private readonly HttpListenerContext context;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpProcessObject"/> class.
		/// </summary>
		/// <param name="context">Http listener context.</param>
		public HttpProcessObject(HttpListenerContext context)
		{
			this.context = context;
		}

		/// <summary>
		/// Gets GTTP request.
		/// </summary>
		public HttpListenerRequest Request
		{
			get
			{
				return context.Request;
			}
		}

		/// <summary>
		/// Gets HTTP response.
		/// </summary>
		public HttpListenerResponse Response
		{
			get
			{
				return context.Response;
			}
		}
	}
}