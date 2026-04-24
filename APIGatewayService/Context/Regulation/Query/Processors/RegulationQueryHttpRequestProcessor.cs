using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using APIGatewayService.Context.RegulationQuery;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using ExternalServiceContracts.Services;

namespace APIGatewayService.Context.Regulation.RegulationQuery.Requests
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation queries. This class is responsible for deserializing the request, validating it, and generating an appropriate response based on the regulation logic. It interacts with the service context for logging and configuration purposes.
	/// </summary>
	internal sealed class RegulationQueryHttpRequestProcessor : BaseHttpRequestProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified service context and a default request validator.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool.</param>
		public RegulationQueryHttpRequestProcessor(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "RegulationQuery",
				triggerPath: "/RegulationQuery/Submit",
				triggerHttpMethod: "POST",
				new RegulationQueryRequestValidator(),
				serviceContext,
				serviceProxyPool)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="httpPrefix">HTTP prefix associated with the request.</param>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		internal RegulationQueryHttpRequestProcessor(
			string httpPrefix,
			string triggerPath,
			string triggerHttpMethod,
			IRequestValidator requestValidator,
			JsonSerializerOptions serializationOptions,
			JsonSerializerOptions deserializationOptions,
			StatelessServiceContext serviceContext,
			IRpServiceProxyPool serviceProxyPool)
				: base(httpPrefix,
					triggerPath,
					triggerHttpMethod,
					requestValidator,
					serializationOptions,
					deserializationOptions,
					serviceContext,
					serviceProxyPool)
		{
		}

		/// <inheritdoc/>
		protected override async Task<IJsonSerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
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
		protected override async Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			RegulationQueryRequest deserializedRequestCasted = (RegulationQueryRequest)deserializedRequest;
			RegulationResponse queryResponse = null;

			try
			{
				queryResponse = await serviceProxyPool.GetProxy<IRegulationQuery>()
					.SubmitQuestion(deserializedRequestCasted).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				LogError("Failed to process regulation query via QueryService", ex);
			}

			if (queryResponse == null)
			{
				queryResponse = RegulationResponse.CreateFailedResponse(0);
			}

			httpResponse.StatusCode = (int)HttpStatusCode.OK;
			httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
			await JsonSerializer.SerializeAsync(httpResponse.OutputStream, queryResponse, serializationOptions).ConfigureAwait(false);
			httpResponse.OutputStream.Close();
			return true;
		}
	}
}