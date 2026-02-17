using APIGatewayService.Common;
using APIGatewayService.Context.Common;
using APIGatewayService.Context.RegulationQuery;
using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;
using System.Fabric;
using System.Text.Json;

namespace APIGatewayService.Context.Regulation.RegulationQuery.Requests
{
	/// <summary>
	/// Represents the processor for handling incoming HTTP requests for regulation queries. This class is responsible for deserializing the request, validating it, and generating an appropriate response based on the regulation logic. It interacts with the service context for logging and configuration purposes.
	/// </summary>
	internal sealed class RegulationQueryHttpRequestProcessor : BaseHttpRequestProcessor<RegulationQueryRequest, RegulationQueryResponse>
	{
		private const string TriggerPath = "/RegulationQuery/Submit";
		private const string TriggerHttpMethod = "POST";

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified service context and a default request validator.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		public RegulationQueryHttpRequestProcessor(StatelessServiceContext serviceContext)
			: base(nameof(RegulationQueryHttpRequestProcessor), new RegulationQueryRequestValidator(), CreateDefaultSerializationOptions(), CreateDefaultDeserializationOptions(), serviceContext)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RegulationQueryHttpRequestProcessor"/> class with the specified request validator and service context.
		/// </summary>
		/// <param name="processorName">Name of processor mostly used for logging.</param>
		/// <param name="requestValidator">Provided request validator.</param>
		/// <param name="serializationOptions">Serialization options.</param>
		/// <param name="deserializationOptions">Deserialization options.</param>
		/// <param name="serviceContext">Service context.</param>
		internal RegulationQueryHttpRequestProcessor(string processorName, IRequestValidator<RegulationQueryRequest> requestValidator, JsonSerializerOptions serializationOptions, JsonSerializerOptions deserializationOptions, StatelessServiceContext serviceContext)
			: base(processorName, requestValidator, serializationOptions, deserializationOptions, serviceContext)
		{
		}

		/// <inheritdoc/>
		public override bool ShouldProcess(HttpProcessObject processObject)
		{
			string path = processObject.Request.Url?.AbsolutePath?.TrimEnd('/') ?? "/";
			return string.Equals(path, TriggerPath, StringComparison.OrdinalIgnoreCase) &&
					string.Equals(processObject.Request.HttpMethod, TriggerHttpMethod, StringComparison.OrdinalIgnoreCase);
		}

		/// <inheritdoc/>
		protected override bool TryCreateResponse(RegulationQueryRequest deserializedRequest, out RegulationQueryResponse deserializedResponse)
		{
			//TODO: Add actual logic to create response based on the deserialized request. For now, we return a default response indicating that the system was unable to provide a response.
			deserializedResponse = new RegulationQueryResponse
			{
				ShortAnswer = "Server was unable to provide response.",
				Explanation = string.Empty,
				Citations = Enumerable.Empty<DocumentCitation>(),
				Confidence = 0f,
			};

			return true;
		}
	}
}