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

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests to retrieve the documents.
	/// </summary>
	internal sealed class GetDocumentsHttpRequestProcessor : BaseHttpRequestProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GetDocumentsHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		public GetDocumentsHttpRequestProcessor(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Documents",
				triggerPath: "/Documents/Get",
				triggerHttpMethod: "GET",
				new EmptyRequestValidator(),
				serviceContext,
				serviceProxyPool)
		{
		}

		/// <inheritdoc/>
		protected override Task<IJsonSerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			return Task.FromResult<IJsonSerializableRequest>(new EmptyRequest());
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			try
			{
				LogInfo("Retrieving all documents from storage.");
				var documents = await serviceProxyPool.GetProxy<IDocumentStorageService>()
					.GetAllDocuments();

				var response = new GetDocumentsInfoResponse
				{
					Documents = documents
				};

				httpResponse.StatusCode = (int)HttpStatusCode.OK;
				httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
				await JsonSerializer.SerializeAsync(httpResponse.OutputStream, response, serializationOptions).ConfigureAwait(false);
				httpResponse.OutputStream.Close();

				return true;
			}
			catch (Exception ex)
			{
				LogError("Failed to retrieve documents via remoting.", ex);
				return false;
			}
		}
	}
}