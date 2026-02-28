using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using APIGatewayService.Context.RegulationQuery;
using CommonSDK;
using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;

namespace APIGatewayService.Context.Regulation.RegulationQuery.Requests
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation queries. This class is responsible for deserializing the request, validating it, and generating an appropriate response based on the regulation logic. It interacts with the service context for logging and configuration purposes.
	/// </summary>
	internal sealed class RegulationQueryHttpRequestProcessor : BaseHttpRequestProcessor
	{
		private const string TriggerPath = "/RegulationQuery/Submit";
		private const string TriggerHttpMethod = "POST";

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified service context and a default request validator.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		public RegulationQueryHttpRequestProcessor(StatelessServiceContext serviceContext)
			: base(TriggerPath,
				TriggerHttpMethod,
				new RegulationQueryRequestValidator(),
				serviceContext)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		internal RegulationQueryHttpRequestProcessor(string triggerPath,
			string triggerHttpMethod,
			IRequestValidator requestValidator,
			JsonSerializerOptions serializationOptions,
			JsonSerializerOptions deserializationOptions,
			StatelessServiceContext serviceContext)
				: base(triggerPath,
					triggerHttpMethod,
					requestValidator,
					serializationOptions,
					deserializationOptions,
					serviceContext)
		{
		}

		/// <inheritdoc/>
		protected override async Task<ISerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			using Stream body = httpRequest.InputStream;
			RegulationQueryRequest? reqParsed = await JsonSerializer.DeserializeAsync<RegulationQueryRequest>(body, deserializationOptions).ConfigureAwait(false);

			if (reqParsed is null)
			{
				throw new InvalidOperationException($"Failed to deserialize as {nameof(RegulationQueryRequest)}: result was null.");
			}

			return reqParsed;
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(ISerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			RegulationQueryRequest deserializedRequestCasted = (RegulationQueryRequest)deserializedRequest;

			//TODO: Add actual logic to create response based on the deserialized request. For now, we return a default response indicating that the system was unable to provide a response.
			RegulationQueryResponse deserializedResponse = new RegulationQueryResponse
			{
				ShortAnswer = "Server was unable to provide response.",
				Explanation = string.Empty,
				Citations = Enumerable.Empty<DocumentCitation>(),
				Confidence = 0f,
			};

			httpResponse.StatusCode = (int)HttpStatusCode.OK;
			httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
			await JsonSerializer.SerializeAsync(httpResponse.OutputStream, deserializedResponse, serializationOptions).ConfigureAwait(false);
			httpResponse.OutputStream.Close();

			return true;
		}
	}
}