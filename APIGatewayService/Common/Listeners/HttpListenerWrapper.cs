using System.Fabric;
using System.Net;
using System.Text;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace APIGatewayService.Common.Listeners
{
	/// <summary>
	/// Wrapper around <see cref="System.Net.HttpListener"/>.
	/// </summary>
	internal class HttpListenerWrapper : ICommunicationListener
	{
		private readonly string endpointName;
		private readonly string listenerName;
		private readonly StatelessServiceContext context;
		private readonly string apiPrefix;
		private readonly IRequestProcessorDispatcher requestProcessorDispatcher;

		private HttpListener? listener;
		private CancellationTokenSource? cts;
		private Task? listenTask;

		/// <summary>
		/// Initializes new instance of <see cref="HttpListenerWrapper"/> with the provided Service Fabric context and sets up request processing."/>
		/// </summary>
		/// <param name="context">Service fabric context.</param>
		/// <param name="endpointName">Name of the endpoint to listen on, as defined in the Service Fabric configuration.</param>
		/// <param name="apiPrefix">API prefix to listen on (e.g., "api/documents").</param>
		/// <param name="requestProcessors">Collection of request processors to register with the listener.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="context"/> is <c>null</c>.</exception>
		public HttpListenerWrapper(StatelessServiceContext context,
			string endpointName,
			string apiPrefix,
			IEnumerable<IRequestProcessor> requestProcessors)
		{
			ArgumentNullException.ThrowIfNull(context);
			ArgumentNullException.ThrowIfNull(endpointName);
			ArgumentNullException.ThrowIfNull(apiPrefix);
			ArgumentNullException.ThrowIfNull(requestProcessors);

			this.apiPrefix = apiPrefix;
			listenerName = GetType().Name;
			requestProcessorDispatcher = new RequestProcessorDispatcher();

			foreach (var requestProcessor in requestProcessors)
			{
				requestProcessorDispatcher.RegisterProcessor(requestProcessor);
			}
		}

		/// <summary>
		/// Starts an HTTP listener on the specified endpoint and returns the URL where the service is accessible.
		/// </summary>
		/// <param name="cancellationToken">A token that can be used to cancel the operation. If cancellation is requested, the listener will stop.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the fully qualified URL where the service is accessible.</returns>
		public Task<string> OpenAsync(CancellationToken cancellationToken)
		{
			int port = context.CodePackageActivationContext.GetEndpoint(endpointName).Port;
			string url = $"http://+:{port}/{apiPrefix}/";

			listener = new HttpListener();
			listener.Prefixes.Add(url);
			listener.Start();

			cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			listenTask = Task.Run(() => ListenLoopAsync(cts.Token), CancellationToken.None);

			string publishedUrl = url.Replace("+", context.NodeContext.IPAddressOrFQDN);
			ServiceEventSource.Current.ServiceMessage(context, $"{listenerName} started on {publishedUrl}");
			return Task.FromResult(publishedUrl);
		}

		/// <summary>
		/// Asynchronously stops the HTTP listener and waits for the background listening task to complete or for the provided cancellation token to be triggered, whichever comes first.
		/// </summary>
		/// <param name="cancellationToken">Token used for cancelling operations.</param>
		/// <returns>Task with closing routine.</returns>
		public Task CloseAsync(CancellationToken cancellationToken)
		{
			StopListener();

			if (listenTask != null)
			{
				return Task.WhenAny(listenTask, Task.Delay(Timeout.Infinite, cancellationToken));
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Aborts the listener immediately without waiting for ongoing operations to complete. This is a forceful shutdown and should be used when an immediate stop is necessary, as it may result in dropped requests or incomplete processing.
		/// </summary>
		public void Abort()
		{
			StopListener();
		}

		/// <summary>
		/// Stops and disposes the internal listener resources.
		/// </summary>
		private void StopListener()
		{
			try
			{
				cts?.Cancel();

				if (listener != null && listener.IsListening)
				{
					listener.Close();
				}
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(context, $"[{listenerName}] (ERROR) Stopping documents listener failed.\n Exception: {ex}");
			}
		}

		/// <summary>
		/// Loops while listening for incoming requests.
		/// </summary>
		/// <param name="cancellationToken">Token used for cancelling operations.</param>
		/// <returns></returns>
		private async Task ListenLoopAsync(CancellationToken cancellationToken)
		{
			if (listener == null)
			{
				ServiceEventSource.Current.ServiceMessage(context, $"[{listenerName}] (ERROR) Starting listening failed (listener is null).");
				return;
			}

			while (!cancellationToken.IsCancellationRequested)
			{
				HttpListenerContext ctx;
				try
				{
					ctx = await listener.GetContextAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					ServiceEventSource.Current.ServiceMessage(context, $"[{listenerName}] (ERROR) Waiting for request failed.\n Exception: {ex}");
					continue;
				}

				_ = Task.Run(() => HandleRequestAsync(ctx), cancellationToken);
			}
		}

		/// <summary>
		/// Handles an individual HTTP request coming from the <see cref="HttpListener"/>. It creates a processing object for the request, dispatches it to the appropriate processor, and handles any exceptions that may occur during processing by returning a 500 Internal Server Error response. Finally, it ensures that the response stream is closed after processing is complete.
		/// </summary>
		/// <param name="ctx">HTTP listener context.</param>
		/// <returns>Task for handling current request.</returns>
		private async Task HandleRequestAsync(HttpListenerContext ctx)
		{
			try
			{
				HttpProcessObject httpRequestProccessObj = new HttpProcessObject(ctx);
				requestProcessorDispatcher.Dispatch(httpRequestProccessObj, out IProcessingResult result);
				return;
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(context, $"[{listenerName}] (ERROR) Processing request failed.\n Exception: {ex}");

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
		/// Writes a JSON string to the provided HTTP response output stream using UTF-8 encoding. Sets the appropriate content type and content length headers before writing the response. After writing, it closes the output stream to complete the response.
		/// </summary>
		/// <param name="response">HTTP response.</param>
		/// <param name="responseMessage">Response message.</param>
		/// <returns>Task with routine for writing response message.</returns>
		private async Task WriteStringResponseAsync(HttpListenerResponse response, string responseMessage)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(responseMessage);
			response.ContentType = ListenerConstants.ResponseTypeUTF8Json;
			response.ContentLength64 = bytes.Length;

			await response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

			response.OutputStream.Close();
		}
	}
}