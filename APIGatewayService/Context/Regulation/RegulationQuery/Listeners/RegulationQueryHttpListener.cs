using APIGatewayService.Common;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Regulation.RegulationQuery.Requests;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System.Fabric;
using System.Net;
using System.Text;

namespace APIGatewayService.Context.Regulation
{
	/// <summary>
	/// HTTP listener that exposes endpoints used to query regulation logic.
	/// </summary>
	/// <remarks>
	/// Implements <see cref="ICommunicationListener"/> so it can be opened/closed by Service Fabric.
	/// The listener currently exposes a POST /RegulationCheck endpoint that accepts a <see cref="RegulationQueryRequest"/>
	/// </remarks>
	internal sealed class RegulationQueryHttpListener : ICommunicationListener
	{
		private const string EndpointName = "ServiceEndpoint";

		private readonly IRequestProcessorDispatcher<HttpProcessObject, HttpProcessResult> requestProcessorDispatcher;
		private readonly StatelessServiceContext context;
		private HttpListener? _listener;
		private CancellationTokenSource? _cts;
		private Task? _listenTask;

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpListener"/> class.
		/// </summary>
		/// <param name="context">The service context used to read configuration and node information.</param>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
		public RegulationQueryHttpListener(StatelessServiceContext context)
		{
			this.context = context ?? throw new ArgumentNullException(nameof(context));
			requestProcessorDispatcher = new RequestProcessorDispatcher<HttpProcessObject, HttpProcessResult>();
			requestProcessorDispatcher.RegisterProcessor(new RegulationQueryHttpRequestProcessor(context));
		}

		/// <summary>
		/// Opens and starts the HTTP listener and returns the published URL.
		/// </summary>
		/// <param name="cancellationToken">A token that can be used to cancel the open operation.</param>
		/// <returns>The published URL where the listener is available.</returns>
		public Task<string> OpenAsync(CancellationToken cancellationToken)
		{
			int port = context.CodePackageActivationContext.GetEndpoint(EndpointName).Port;
			string url = $"http://+:{port}/RegulationQuery/";

			_listener = new HttpListener();
			_listener.Prefixes.Add(url);
			_listener.Start();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			// Start background loop
			_listenTask = Task.Run(() => ListenLoopAsync(_cts.Token), CancellationToken.None);

			// Replace '+' with actual node address for published address
			string publishedUrl = url.Replace("+", context.NodeContext.IPAddressOrFQDN);
			ServiceEventSource.Current.ServiceMessage(context, "RegulationHttpListener started on {0}", publishedUrl);
			return Task.FromResult(publishedUrl);
		}

		/// <summary>
		/// Closes the listener and waits for the background listen task to complete or for the provided cancellation token to be signaled.
		/// </summary>
		/// <param name="cancellationToken">A token used to cancel waiting for the listener to close.</param>
		/// <returns>A task that completes when the close operation finishes or the cancellation token is triggered.</returns>
		public Task CloseAsync(CancellationToken cancellationToken)
		{
			StopListener();

			// Wait for listen task to finish, but respect the cancellation token
			if (_listenTask != null)
			{
				return Task.WhenAny(_listenTask, Task.Delay(Timeout.Infinite, cancellationToken));
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Aborts the listener immediately. Intended for use when the host needs to abort the service.
		/// </summary>
		public void Abort()
		{
			StopListener();
		}

		/// <summary>
		/// Stops and disposes the internal listener resources. Safe to call multiple times.
		/// </summary>
		private void StopListener()
		{
			try
			{
				_cts?.Cancel();

				if (_listener != null && _listener.IsListening)
				{
					_listener.Close();
				}
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(context, "Exception stopping listener: {0}", ex);
			}
		}

		/// <summary>
		/// Background loop that accepts incoming <see cref="HttpListenerContext"/> requests and dispatches handlers.
		/// </summary>
		/// <param name="token">Cancellation token used to break out of the loop.</param>
		/// <returns>A <see cref="Task"/> representing the listen loop lifetime.</returns>
		private async Task ListenLoopAsync(CancellationToken token)
		{
			if (_listener == null) return;

			while (!token.IsCancellationRequested)
			{
				HttpListenerContext ctx;
				try
				{
					ctx = await _listener.GetContextAsync().ConfigureAwait(false);
				}
				catch (HttpListenerException)
				{
					// Listener was closed
					break;
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					ServiceEventSource.Current.ServiceMessage(context, "Listener exception: {0}", ex);
					continue;
				}

				_ = Task.Run(() => HandleRequestAsync(ctx), token);
			}
		}

		/// <summary>
		/// Handles an individual HTTP request coming from the <see cref="HttpListener"/>.
		/// </summary>
		/// <param name="ctx">The HTTP listener context for the incoming request.</param>
		/// <returns>A <see cref="Task"/> that completes when the request has been processed and response written.</returns>
		private async Task HandleRequestAsync(HttpListenerContext ctx)
		{
			try
			{
				HttpProcessObject httpRequestProccessObj = new HttpProcessObject(ctx);
				requestProcessorDispatcher.Dispatch(httpRequestProccessObj, out HttpProcessResult result);
				return;
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(context, "Request handling exception: {0}", ex);
				try
				{
					ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					await WriteStringResponseAsync(ctx.Response, "{\"error\":\"server_error\"}").ConfigureAwait(false);
				}
				catch
				{
				}
			}
			finally
			{
				try
				{
					ctx.Response.OutputStream.Close();
				}
				catch
				{
				}
			}
		}

		/// <summary>
		/// Writes a JSON string to the provided <see cref="HttpListenerResponse"/> output stream using UTF-8 encoding.
		/// </summary>
		/// <param name="response">The HTTP response to write to.</param>
		/// <param name="content">The JSON string content to write.</param>
		/// <returns>A <see cref="Task"/> that completes when the response has been written.</returns>
		private async Task WriteStringResponseAsync(HttpListenerResponse response, string content)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(content);
			response.ContentType = "application/json; charset=utf-8";
			response.ContentLength64 = bytes.Length;
			await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
			response.OutputStream.Close();
		}
	}
}