using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Services;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests to delete a specific document by title and version.
	/// </summary>
	internal sealed class DeleteDocumentHttpRequestProcessor : BaseHttpRequestProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteDocumentHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		public DeleteDocumentHttpRequestProcessor(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Documents",
				triggerPath: "/Documents/Delete",
				triggerHttpMethod: "POST",
				new DeleteDocumentRequestValidator(),
				serviceContext,
				serviceProxyPool)
		{
		}

		/// <inheritdoc/>
		protected override async Task<IJsonSerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
		{
			using Stream body = httpRequest.InputStream;
			DeleteDocumentRequest? reqParsed = await JsonSerializer.DeserializeAsync<DeleteDocumentRequest>(body, deserializationOptions).ConfigureAwait(false);

			if (reqParsed == null)
			{
				throw new InvalidOperationException($"Failed to deserialize as {nameof(DeleteDocumentRequest)}: result was null.");
			}

			return reqParsed;
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			DeleteDocumentRequest deserializedRequestCasted = (DeleteDocumentRequest)deserializedRequest;

			try
			{
				LogInfo($"Deleting document: {deserializedRequestCasted.Title} v{deserializedRequestCasted.VersionNumber}");
				DeleteDocumentResponse response = await serviceProxyPool.GetProxy<IDocumentStorageService>()
					.DeleteDocument(deserializedRequestCasted);

				httpResponse.StatusCode = response.Success ? (int)HttpStatusCode.OK : (int)HttpStatusCode.NotFound;
				httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
				await JsonSerializer.SerializeAsync(httpResponse.OutputStream, response, serializationOptions).ConfigureAwait(false);
				httpResponse.OutputStream.Close();

				return true;
			}
			catch (Exception ex)
			{
				LogError("Failed to delete document via remoting.", ex);
				return false;
			}
		}
	}
}