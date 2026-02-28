using System.Fabric;
using System.Net;
using System.Text.Json;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.Processors;
using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Processor for handling HTTP requests related to document uploads.
	/// </summary>
	internal sealed class DocumentUploadHttpRequestProcessor : BaseHttpRequestProcessor
	{
		private const string TriggerPath = "/Documents/Add";
		private const string TriggerHttpMethod = "POST";

		private readonly IDocumentStorageService documentStorageServiceProxy;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentUploadHttpRequestProcessor"/> class.
		/// </summary>
		/// <param name="serviceContext">Service context.</param>
		public DocumentUploadHttpRequestProcessor(StatelessServiceContext serviceContext)
			: base(TriggerPath,
				TriggerHttpMethod,
				new DocumentUploadRequestValidator(),
				serviceContext)
		{
			documentStorageServiceProxy = CreateDocumentStorageServiceProxy();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentUploadHttpRequestProcessor"/> class with the specified request validator, serialization options, and service context.
		/// </summary>
		/// <param name="triggerHttpMethod">Method to which processor responds.</param>
		/// <param name="triggerPath">API path of request.</param>
		/// <param name="requestValidator">The validator used to validate incoming requests.</param>
		/// <param name="serializationOptions">The options for serializing responses.</param>
		/// <param name="deserializationOptions">The options for deserializing requests.</param>
		/// <param name="serviceContext">The Service Fabric context for logging and configuration.</param>
		internal DocumentUploadHttpRequestProcessor(string triggerPath,
			string triggerHttpMethod,
			IRequestValidator requestValidator,
			JsonSerializerOptions serializationOptions,
			JsonSerializerOptions deserializationOptions,
			StatelessServiceContext serviceContext,
			IDocumentStorageService documentStorageServiceProxy)
				: base(triggerPath,
					triggerHttpMethod,
					requestValidator,
					serializationOptions,
					deserializationOptions,
					serviceContext)
		{
			this.documentStorageServiceProxy = documentStorageServiceProxy;
		}

		/// <inheritdoc/>
		protected override async Task<ISerializableRequest> ParseRequest(HttpListenerRequest httpRequest)
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
		protected override async Task<bool> TryCreateResponse(ISerializableRequest deserializedRequest, HttpListenerResponse httpResponse)
		{
			DocumentUploadRequest deserializedRequestCasted = (DocumentUploadRequest)deserializedRequest;
			DocumentUploadResponse deserializedResponse = new DocumentUploadResponse();

			try
			{
				LogInfo($"Received document upload: {deserializedRequestCasted.Title} (bytes={deserializedRequestCasted.FileBytes?.Length ?? 0})");
				DocumentItemDescriptor uploadedItemDesciptor = await documentStorageServiceProxy.StoreDocument(deserializedRequestCasted);

				if (uploadedItemDesciptor == null)
				{
					LogError("Document storage service failed to store document via remoting.");
					return false;
				}

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

		/// <summary>
		/// Create s proxy for calling document storage service.
		/// </summary>
		/// <returns>Proxy for calling document storage service.</returns>
		private IDocumentStorageService CreateDocumentStorageServiceProxy()
		{
			try
			{
				var serviceUri = new Uri("fabric:/RegulationAssistent/DocumentStorageService");
				return ServiceProxy.Create<IDocumentStorageService>(serviceUri, new ServicePartitionKey(0));
			}
			catch (Exception ex)
			{
				LogError($"Failed to create proxy of type {nameof(IDocumentStorageService)}.", ex);
				throw;
			}
		}
	}
}