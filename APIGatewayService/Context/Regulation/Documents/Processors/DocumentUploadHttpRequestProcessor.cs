using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;
using System.Linq;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests related to document uploads.
	/// </summary>
	internal sealed class DocumentUploadHttpRequestProcessor : BaseHttpRequestProcessor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentUploadHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		public DocumentUploadHttpRequestProcessor(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
			: base(httpPrefix: "Documents",
					triggerPath: "/Documents/Add",
					triggerHttpMethod: "POST",
					new DocumentUploadRequestValidator(serviceContext, serviceProxyPool),
					serviceContext,
					serviceProxyPool)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentUploadHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="httpPrefix">HTTP prefix associated with the request.</param>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		/// <param name="serviceProxyPool">Service proxy pool for accessing service proxies.</param>
		internal DocumentUploadHttpRequestProcessor(string httpPrefix,
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
			DocumentUploadRequest? reqParsed = await JsonSerializer.DeserializeAsync<DocumentUploadRequest>(body, deserializationOptions).ConfigureAwait(false);

			if (reqParsed == null)
			{
				throw new InvalidOperationException($"Failed to deserialize as {nameof(DocumentUploadRequest)}: result was null.");
			}

			return reqParsed;
		}

		/// <inheritdoc/>
		protected override async Task<bool> TryCreateResponse(IJsonSerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			DocumentUploadRequest deserializedRequestCasted = (DocumentUploadRequest)deserializedRequest;
			DocumentUploadResponse deserializedResponse = new DocumentUploadResponse();

			try
			{
				LogInfo($"Received document upload: {deserializedRequestCasted.Title} (bytes={deserializedRequestCasted.FileBytes?.Length ?? 0})");

				// Call storage service which now returns StoreDocumentResponse
				StoreDocumentResponse storeResponse = await serviceProxyPool.GetProxy<IDocumentStorageService>().StoreDocument(deserializedRequestCasted);

				if (storeResponse == null || !storeResponse.Success || storeResponse.DocumentDescriptor == null)
				{
					LogError("Document storage service failed to store document via remoting.");
					return false;
				}

				DocumentItemDescriptor uploadedItemDesciptor = storeResponse.DocumentDescriptor;

				deserializedResponse.DocumentDescriptor = uploadedItemDesciptor;

				httpResponse.StatusCode = (int)HttpStatusCode.OK;
				httpResponse.ContentType = ListenerConstants.ResponseTypeUTF8Json;
				await JsonSerializer.SerializeAsync(httpResponse.OutputStream, deserializedResponse, serializationOptions).ConfigureAwait(false);
				httpResponse.OutputStream.Close();

				return true;
			}
			catch (Exception ex)
			{
				LogError("Failed to store document via remoting.", ex);
				return false;
			}
		}
	}
}