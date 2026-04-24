using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Responses;
using ExternalServiceContracts.Services;

namespace APIGatewayService.Context.Regulation.RegulationQuery.Requests
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation query tracing.
	/// </summary>
	internal sealed class AuditHttpRequestProcessor : BaseHttpRequestProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuditHttpRequestProcessor"/> class with the specified service context and a default request validator.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool.</param>
		public AuditHttpRequestProcessor(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Auditing",
				triggerPath: "/Auditing/TraceLastRequest",
				triggerHttpMethod: "POST",
				requestValidator: null,
				serviceContext,
				serviceProxyPool)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuditHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="httpPrefix">HTTP prefix associated with the request.</param>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		internal AuditHttpRequestProcessor(
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
			return new EmptyRequest();
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			RegulationQueryTraceResponse traceResponse = new RegulationQueryTraceResponse();

			try
			{
				var traceString = await serviceProxyPool.GetProxy<IAuditService>().TraceLastRequest();
				traceResponse.Trace = traceString;
			}
			catch (Exception ex)
			{
				LogError("Failed to process regulation query via QueryService", ex);
			}

			httpResponse.StatusCode = (int)HttpStatusCode.OK;
			httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
			await JsonSerializer.SerializeAsync(httpResponse.OutputStream, traceResponse, serializationOptions).ConfigureAwait(false);
			httpResponse.OutputStream.Close();
			return true;
		}
	}
}