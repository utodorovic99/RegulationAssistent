using System.Fabric;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Context.Common;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Serialization;

namespace APIGatewayService.Common.Processors
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation queries.
	/// This class is responsible for deserializing the request, validating it, and generating
	/// an appropriate response based on the regulation logic. It interacts with the service
	/// context for logging and configuration purposes.
	/// </summary>
	internal abstract class BaseHttpRequestProcessor : IHttpRequestProcessor
	{
		protected readonly string httpPrefix;
		protected readonly string triggerPath;
		protected readonly string triggerHttpMethod;
		protected readonly StatelessServiceContext serviceContext;
		protected readonly JsonSerializerOptions serializationOptions;
		protected readonly JsonSerializerOptions deserializationOptions;
		protected readonly IRpServiceProxyPool serviceProxyPool;

		private readonly string processorName;
		private readonly IRequestValidator requestValidator;

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseHttpRequestProcessor"/> class with the specified request validator and service context.
		/// </summary>
		/// <param name="httpPrefix">HTTP prefix associated with the request.</param>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		/// /// <param name="proxyPool">Service proxy pool.</param>
		public BaseHttpRequestProcessor(string httpPrefix,
			string triggerPath,
			string triggerHttpMethod,
			IRequestValidator requestValidator,
			StatelessServiceContext serviceContext,
			IRpServiceProxyPool proxyPool)
			: this(httpPrefix,
				triggerPath,
				triggerHttpMethod,
				requestValidator,
				CreateDefaultSerializationOptions(),
				CreateDefaultDeserializationOptions(),
				serviceContext,
				proxyPool)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="BaseHttpRequestProcessor"/>.
		/// </summary>
		/// <param name="httpPrefix">HTTP prefix associated with the request.</param>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		/// <param name="proxyPool">Service proxy pool.</param>
		internal BaseHttpRequestProcessor(string httpPrefix,
			string triggerPath,
			string triggerHttpMethod,
			IRequestValidator requestValidator,
			JsonSerializerOptions serializationOptions,
			JsonSerializerOptions deserializationOptions,
			StatelessServiceContext serviceContext,
			IRpServiceProxyPool proxyPool)
		{
			this.httpPrefix = httpPrefix;
			this.triggerPath = triggerPath;
			this.triggerHttpMethod = triggerHttpMethod;
			this.processorName = this.GetType().Name;
			this.requestValidator = requestValidator;
			this.serializationOptions = serializationOptions;
			this.deserializationOptions = deserializationOptions;
			this.serviceContext = serviceContext;
			this.serviceProxyPool = proxyPool;
		}

		/// <inheritdoc/>
		public string HttpPrefix
		{
			get
			{
				return httpPrefix;
			}
		}

		/// <inheritdoc/>
		public bool ShouldProcess(IProcessingObject processObject)
		{
			HttpProcessObject processObjectCasted = (HttpProcessObject)processObject;

			string path = processObjectCasted.Request.Url?.AbsolutePath?.TrimEnd('/') ?? "/";
			return string.Equals(path, triggerPath, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(processObjectCasted.Request.HttpMethod, triggerHttpMethod, StringComparison.OrdinalIgnoreCase);
		}

		/// <inheritdoc/>
		public async Task<IProcessingResult> ProcessRequestAsync(IProcessingObject processObject)
		{
			HttpProcessObject processObjectCasted = (HttpProcessObject)processObject;

			IJsonSerializableRequest deserializedRequest;
			try
			{
				deserializedRequest = await ParseRequest(processObjectCasted.Request);
			}
			catch (Exception ex)
			{
				LogError("Failed to deserialize request", ex);

				return await HandleFailedResult(
					processObjectCasted.Response,
					errMessage: $"invalid_json: {ex.Message}");
			}

			if (!requestValidator.TryValidateRequest(deserializedRequest, out string validationError))
			{
				LogError("Validation failed for the request", new InvalidOperationException(validationError));

				return await HandleFailedResult(
					processObjectCasted.Response,
					errMessage: validationError);
			}

			LogInfo($"Request received: {deserializedRequest}");
			bool result = await TryCreateResponse(deserializedRequest, processObjectCasted.Response);

			if (!result)
			{
				LogError("Failed to create response");

				return await HandleFailedResult(
					processObjectCasted.Response,
					errMessage: "Failed to create response.");
			}

			return await Task.FromResult(StatusProcessResult.Success);
		}

		/// <summary>
		/// Handles a failed processing result by setting the HTTP response status code to 400 Bad Request.
		/// </summary>
		/// <param name="httpResponse">The HTTP response object.</param>
		/// <param name="errMessage">The error message to include in the response body.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		protected async Task<StatusProcessResult> HandleFailedResult(HttpListenerResponse httpResponse, string errMessage)
		{
			httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
			await WriteStringResponseAsync(httpResponse, $"{{\"error\":\"{errMessage}\"}}").ConfigureAwait(false);

			return StatusProcessResult.Failed;
		}

		/// <summary>
		/// Logs error message.
		/// </summary>
		/// <param name="message">Message to log.</param>
		/// <param name="ex">Exception to log.</param>
		protected void LogError(string message, Exception ex = null)
		{
			if (ex == null)
			{
				ServiceEventSource.Current.ServiceMessage(serviceContext, $"(ERROR) {processorName}: {message}.");
			}
			else
			{
				ServiceEventSource.Current.ServiceMessage(serviceContext, $"[{processorName}] (ERROR): {message}.\nException: {ex}");
			}
		}

		/// <summary>
		/// Logs info message.
		/// </summary>
		/// <param name="message">Message to log.</param>
		protected void LogInfo(string message)
		{
			ServiceEventSource.Current.ServiceMessage(serviceContext, $"[{processorName}]: {message}");
		}

		/// <summary>
		/// Parses the incoming HTTP request body as JSON and deserializes it into an object of type <typeparamref name="TDeserializedRequest"/>.
		/// </summary>
		/// <param name="httpRequest">The HTTP request object.</param>
		/// <returns>The deserialized request object.</returns>
		/// <exception cref="NullReferenceException">Thrown if deserialization fails.</exception>
		protected abstract Task<IJsonSerializableRequest> ParseRequest(HttpListenerRequest httpRequest);

		/// <summary>
		/// Creates response.
		/// </summary>
		/// <param name="deserializedRequest">Deserialized request.</param>
		/// <param name="httpResponse">HTTP response where deserialized response will be stored.</param>
		/// <returns><c>True</c> if response is successfully created. Otherwise returns <c>false</c>.</returns>
		protected abstract Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse);

		/// <summary>
		/// Creates and configures default options for deserializing JSON requests.
		/// </summary>
		/// <returns>A configured <see cref="JsonSerializerOptions"/> instance.</returns>
		protected static JsonSerializerOptions CreateDefaultDeserializationOptions()
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
			};

			options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
			// Ensure DateOnly is (de)serialized consistently
			options.Converters.Add(new DateOnlyJsonConverterFactory());

			return options;
		}

		/// <summary>
		/// Creates and configures default options for serializing JSON responses.
		/// </summary>
		/// <returns>A configured <see cref="JsonSerializerOptions"/> instance.</returns>
		protected static JsonSerializerOptions CreateDefaultSerializationOptions()
		{
			var options = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			};

			// Ensure DateOnly is serialized consistently
			options.Converters.Add(new DateOnlyJsonConverterFactory());

			return options;
		}

		/// <summary>
		/// Writes a JSON string to the provided HTTP response output stream using UTF-8 encoding.
		/// </summary>
		/// <param name="httpResponse">The HTTP response object.</param>
		/// <param name="content">The JSON string content to write.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task WriteStringResponseAsync(HttpListenerResponse httpResponse, string content)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(content);
			httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
			httpResponse.ContentLength64 = bytes.Length;
			await httpResponse.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

			httpResponse.OutputStream.Close();
		}
	}
}