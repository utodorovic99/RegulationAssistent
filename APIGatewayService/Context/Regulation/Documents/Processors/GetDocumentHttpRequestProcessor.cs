using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Common.ServiceProxies;
using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests to retrieve a specific document by title and version.
	/// </summary>
	internal sealed class GetDocumentHttpRequestProcessor : BaseHttpRequestProcessor
	{
		private readonly ServiceProxyPool serviceProxyPool;

		/// <summary>
		/// Initializes a new instance of the <see cref="GetDocumentHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		public GetDocumentHttpRequestProcessor(StatelessServiceContext serviceContext, ServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Documents",
				triggerPath: "/Documents/GetDocument",
				triggerHttpMethod: "POST",
				new GetDocumentRequestValidator(),
				serviceContext)
		{
			this.serviceProxyPool = serviceProxyPool ?? throw new ArgumentNullException(nameof(serviceProxyPool));
		}

		/// <inheritdoc/>
		protected override async Task<ISerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			using Stream body = httpRequest.InputStream;
			GetDocumentRequest? reqParsed = await JsonSerializer.DeserializeAsync<GetDocumentRequest>(body, deserializationOptions).ConfigureAwait(false);

			if (reqParsed == null)
			{
				throw new InvalidOperationException($"Failed to deserialize as {nameof(GetDocumentRequest)}: result was null.");
			}

			return reqParsed;
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(ISerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			GetDocumentRequest deserializedRequestCasted = (GetDocumentRequest)deserializedRequest;

			try
			{
				LogInfo($"Retrieving document: {deserializedRequestCasted.Title} v{deserializedRequestCasted.VersionNumber}");
				GetDocumentResponse? response = await serviceProxyPool.DocumentStorageService.GetDocument(deserializedRequestCasted);

				if (response == null)
				{
					LogError("Document not found.");
					return false;
				}

				httpResponse.StatusCode = (int)HttpStatusCode.OK;
				httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
				await JsonSerializer.SerializeAsync(httpResponse.OutputStream, response, serializationOptions).ConfigureAwait(false);
				httpResponse.OutputStream.Close();

				return true;
			}
			catch (Exception ex)
			{
				LogError("Failed to retrieve document via remoting.", ex);
				return false;
			}
		}
	}
}
