using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Requests;
using System.Fabric;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace APIGatewayService.Common
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation queries.
	/// This class is responsible for deserializing the request, validating it, and generating
	/// an appropriate response based on the regulation logic. It interacts with the service
	/// context for logging and configuration purposes.
	/// </summary>
	internal abstract class BaseHttpRequestProcessor<TDeserializedRequest, TDeserializedResponse> : IRequestProcessor<HttpProcessObject, HttpProcessResult>
		where TDeserializedRequest : ISerializableRequest
		where TDeserializedResponse : ISerializableResponse
	{
		private readonly string processorName;
		private readonly IRequestValidator<RegulationQueryRequest> requestValidator;
		private readonly StatelessServiceContext serviceContext;
		private readonly JsonSerializerOptions serializationOptions;
		private readonly JsonSerializerOptions deserializationOptions;

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseHttpRequestProcessor"/> class with the specified request validator and service context.
		/// </summary>
		/// <param name="processorName">The name of the processor, primarily used for logging.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		public BaseHttpRequestProcessor(string processorName, IRequestValidator<RegulationQueryRequest> requestValidator, StatelessServiceContext serviceContext)
			: this(processorName, requestValidator, CreateDefaultSerializationOptions(), CreateDefaultDeserializationOptions(), serviceContext)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BaseHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="processorName">The name of the processor, primarily used for logging.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		internal BaseHttpRequestProcessor(string processorName, IRequestValidator<RegulationQueryRequest> requestValidator, JsonSerializerOptions serializationOptions, JsonSerializerOptions deserializationOptions, StatelessServiceContext serviceContext)
		{
			this.processorName = processorName;
			this.requestValidator = requestValidator;
			this.serializationOptions = serializationOptions;
			this.deserializationOptions = deserializationOptions;
			this.serviceContext = serviceContext;
		}

		/// <inheritdoc/>
		public abstract bool ShouldProcess(HttpProcessObject processObject);

		/// <inheritdoc/>
		public async Task<HttpProcessResult> ProcessRequestAsync(HttpProcessObject processObject)
		{
			HttpListenerRequest httpRequest = processObject.Request;
			HttpListenerResponse httpResponse = processObject.Response;

			TDeserializedRequest? deserializedRequest = default;
			try
			{
				deserializedRequest = await ParseRequest(httpRequest);
			}
			catch (Exception ex)
			{
				return await HandleFailedResult(
					httpResponse,
					errMessage: $"invalid_json: {ex.Message}",
					logMessage: "[{0}] Failed to deserialize request. Exception: {1}",
					logMessageArgs: [processorName, ex]);
			}

			// Run validations
			if (!requestValidator.TryValidateRequest(deserializedRequest, out string validationError))
			{
				return await HandleFailedResult(
					httpResponse,
					errMessage: validationError,
					logMessage: "[{0}] Validation failed for the request. Error: {1}",
					logMessageArgs: [processorName, validationError]);
			}

			// Log the received question and minimal context info
			ServiceEventSource.Current.ServiceMessage(serviceContext, "[{0}] Request received: {1}", processorName, deserializedRequest);

			if (!TryCreateResponse(deserializedRequest, out TDeserializedResponse deserializedResponse))
			{
				return await HandleFailedResult(
					httpResponse,
					errMessage: "Failed to create response.",
					logMessage: "[{0}] Response creation failed.",
					logMessageArgs: [processorName]);
			}

			return await HandleSuccessfulResult(httpResponse, deserializedResponse);
		}

		/// <summary>
		/// Creates a deserialized response object based on the provided deserialized request.
		/// This method encapsulates the core logic for processing the request and generating
		/// the appropriate response. It returns a boolean indicating whether the response
		/// creation was successful, and outputs the deserialized response object if successful.
		/// </summary>
		/// <param name="deserializedRequest">The deserialized request object.</param>
		/// <param name="deserializedResponse">The deserialized response object.</param>
		/// <returns><c>true</c> if the response was successfully created; otherwise, <c>false</c>.</returns>
		protected abstract bool TryCreateResponse(TDeserializedRequest deserializedRequest, out TDeserializedResponse deserializedResponse);

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

			// Allow enum values to be provided as strings (e.g. "en")
			options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

			return options;
		}

		/// <summary>
		/// Creates and configures default options for serializing JSON responses.
		/// </summary>
		/// <returns>A configured <see cref="JsonSerializerOptions"/> instance.</returns>
		protected static JsonSerializerOptions CreateDefaultSerializationOptions()
		{
			return new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			};
		}

		/// <summary>
		/// Handles a successful processing result by setting the HTTP response status code to 200 OK
		/// and writing the provided deserialized response object as JSON to the response output stream.
		/// </summary>
		/// <param name="httpResponse">The HTTP response object.</param>
		/// <param name="deserializedResponse">The deserialized response object to include in the response body.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		private async Task<HttpProcessResult> HandleSuccessfulResult(HttpListenerResponse httpResponse, TDeserializedResponse deserializedResponse)
		{
			httpResponse.StatusCode = (int)HttpStatusCode.OK;
			httpResponse.ContentType = "application/json; charset=utf-8";
			await JsonSerializer.SerializeAsync(httpResponse.OutputStream, deserializedResponse, serializationOptions).ConfigureAwait(false);
			httpResponse.OutputStream.Close();

			return HttpProcessResult.Success;
		}

		/// <summary>
		/// Handles a failed processing result by setting the HTTP response status code to 400 Bad Request
		/// and writing an error message as JSON to the response output stream.
		/// </summary>
		/// <param name="httpResponse">The HTTP response object.</param>
		/// <param name="errMessage">The error message to include in the response body.</param>
		/// <param name="logMessage">The log message template for logging the error.</param>
		/// <param name="logMessageArgs">Arguments for the log message template.</param>
		/// <returns>A task representing the asynchronous operation.</returns>
		protected async Task<HttpProcessResult> HandleFailedResult(HttpListenerResponse httpResponse, string errMessage, string logMessage, params object[] logMessageArgs)
		{
			if (logMessage is not null)
			{
				ServiceEventSource.Current.ServiceMessage(serviceContext, logMessage, logMessageArgs);
			}

			httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
			await WriteStringResponseAsync(httpResponse, $"{{\"error\":\"{errMessage}\"}}").ConfigureAwait(false);

			return HttpProcessResult.Failed;
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
			httpResponse.ContentType = "application/json; charset=utf-8";
			httpResponse.ContentLength64 = bytes.Length;
			await httpResponse.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

			httpResponse.OutputStream.Close();
		}

		/// <summary>
		/// Parses the incoming HTTP request body as JSON and deserializes it into an object of type <typeparamref name="TDeserializedRequest"/>.
		/// </summary>
		/// <param name="httpRequest">The HTTP request object.</param>
		/// <returns>The deserialized request object.</returns>
		/// <exception cref="NullReferenceException">Thrown if deserialization fails.</exception>
		private async Task<TDeserializedRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			using Stream body = httpRequest.InputStream;
			TDeserializedRequest? reqParsed = await JsonSerializer.DeserializeAsync<TDeserializedRequest>(body, deserializationOptions).ConfigureAwait(false);

			if (reqParsed is null)
			{
				throw new NullReferenceException($"Failed to deserialize as {typeof(TDeserializedRequest).Name}: result was null.");
			}

			return reqParsed;
		}
	}
}