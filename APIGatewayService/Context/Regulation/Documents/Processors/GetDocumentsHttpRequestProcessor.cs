using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Common.ServiceProxies;
using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Responses;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests to retrieve the documents.
	/// </summary>
	internal sealed class GetDocumentsHttpRequestProcessor : BaseHttpRequestProcessor
	{
		private readonly ServiceProxyPool serviceProxyPool;

		/// <summary>
		/// Initializes a new instance of the <see cref="GetDocumentsHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		public GetDocumentsHttpRequestProcessor(StatelessServiceContext serviceContext, ServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Documents",
				triggerPath: "/Documents/Get",
				triggerHttpMethod: "GET",
				new EmptyRequestValidator(),
				serviceContext)
		{
			this.serviceProxyPool = serviceProxyPool ?? throw new ArgumentNullException(nameof(serviceProxyPool));
		}

		/// <inheritdoc/>
		protected override Task<ISerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			return Task.FromResult<ISerializableRequest>(new EmptyRequest());
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(ISerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			try
			{
				LogInfo("Retrieving all documents from storage.");
				var documents = await serviceProxyPool.DocumentStorageService.GetAllDocuments();

				var response = new GetDocumentsResponse
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
