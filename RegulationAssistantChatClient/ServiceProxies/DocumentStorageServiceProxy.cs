using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using RegulationAssistantChatClient.Configuration;

namespace RegulationAssistantChatClient.Services
{
	/// <summary>
	/// Proxy used to send document upload requests to the Document Storage service endpoint via API Gateway.
	/// Encapsulates HTTP interactions and deserialization of the response.
	/// </summary>
	public class DocumentStorageServiceProxy
	{
		private readonly string serviceUrl;
		private readonly HttpClient httpClient;

		/// <summary>
		/// Creates a new instance of <see cref="DocumentStorageServiceProxy"/>.
		/// </summary>
		/// <param name="httpClient">An <see cref="HttpClient"/> instance used to perform HTTP requests. Must not be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
		public DocumentStorageServiceProxy(HttpClient httpClient)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			serviceUrl = AppSettingsProvider.Settings.DocumentStorageServiceBaseUrl;
		}

		/// <summary>
		/// Sends a <see cref="DocumentUploadRequest"/> to the remote Document Storage service via API Gateway and
		/// returns the deserialized <see cref="DocumentUploadResponse"/>.
		/// </summary>
		/// <param name="request">The document upload request containing document metadata and file bytes.</param>
		/// <returns>
		/// The deserialized <see cref="DocumentUploadResponse"/> containing the document descriptor when the call succeeds; 
		/// otherwise null when an error occurs.
		/// </returns>
		/// <remarks>
		/// The method performs a POST to the configured service base URL using the path "Add".
		/// Any exception during sending or deserialization is caught and results in returning null.
		/// Callers should check for null and handle failures accordingly.
		/// </remarks>
		public async Task<DocumentUploadResponse?> UploadDocumentAsync(DocumentUploadRequest request)
		{
			try
			{
				string requestUrl = "Add";

				string jsonContent = JsonSerializer.Serialize(request);
				HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await httpClient.PostAsync($"{serviceUrl}/{requestUrl}", httpContent);
				response.EnsureSuccessStatusCode();

				string responseContent = await response.Content.ReadAsStringAsync();

				var deserializationOptions = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				DocumentUploadResponse? deserializedResponse = JsonSerializer.Deserialize<DocumentUploadResponse>(responseContent, deserializationOptions);
				return deserializedResponse;
			}
			catch (Exception ex)
			{
				// Log or handle exception as needed
				return null;
			}
		}

		/// <summary>
		/// Retrieves all documents from the Document Storage service via API Gateway.
		/// </summary>
		/// <returns>
		/// A list of <see cref="DocumentItemDescriptor"/> when the call succeeds; 
		/// otherwise an empty list when an error occurs.
		/// </returns>
		/// <remarks>
		/// The method performs a GET to the configured service base URL using the path "Get".
		/// Any exception during the request or deserialization is caught and results in returning an empty list.
		/// </remarks>
		public async Task<List<DocumentItemDescriptor>> GetAllDocumentsAsync()
		{
			try
			{
				string requestUrl = "Get";

				HttpResponseMessage response = await httpClient.GetAsync($"{serviceUrl}/{requestUrl}");
				response.EnsureSuccessStatusCode();

				string responseContent = await response.Content.ReadAsStringAsync();

				var deserializationOptions = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				GetDocumentsResponse? deserializedResponse = JsonSerializer.Deserialize<GetDocumentsResponse>(responseContent, deserializationOptions);
				return deserializedResponse?.Documents ?? new List<DocumentItemDescriptor>();
			}
			catch (Exception ex)
			{
				// Log or handle exception as needed
				return new List<DocumentItemDescriptor>();
			}
		}

		/// <summary>
		/// Retrieves a specific document's bytes by title and version number.
		/// </summary>
		/// <param name="title">The title of the document to retrieve.</param>
		/// <param name="versionNumber">The version number of the document to retrieve.</param>
		/// <returns>
		/// The deserialized <see cref="GetDocumentResponse"/> containing document bytes when the call succeeds;
		/// otherwise null when an error occurs.
		/// </returns>
		public async Task<GetDocumentResponse?> GetDocumentAsync(string title, int versionNumber)
		{
			try
			{
				string requestUrl = "GetDocument";

				var request = new GetDocumentRequest
				{
					Title = title,
					VersionNumber = versionNumber
				};

				string jsonContent = JsonSerializer.Serialize(request);
				HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await httpClient.PostAsync($"{serviceUrl}/{requestUrl}", httpContent);
				response.EnsureSuccessStatusCode();

				string responseContent = await response.Content.ReadAsStringAsync();

				var deserializationOptions = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				GetDocumentResponse? deserializedResponse = JsonSerializer.Deserialize<GetDocumentResponse>(responseContent, deserializationOptions);
				return deserializedResponse;
			}
			catch (Exception ex)
			{
				// Log or handle exception as needed
				return null;
			}
		}

		/// <summary>
		/// Deletes a specific document by title and version number.
		/// </summary>
		/// <param name="title">The title of the document to delete.</param>
		/// <param name="versionNumber">The version number of the document to delete.</param>
		/// <returns>
		/// The deserialized <see cref="DeleteDocumentResponse"/> indicating success or failure;
		/// otherwise null when an error occurs.
		/// </returns>
		public async Task<DeleteDocumentResponse?> DeleteDocumentAsync(string title, int versionNumber)
		{
			try
			{
				string requestUrl = "Delete";

				var request = new DeleteDocumentRequest
				{
					Title = title,
					VersionNumber = versionNumber
				};

				string jsonContent = JsonSerializer.Serialize(request);
				HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await httpClient.PostAsync($"{serviceUrl}/{requestUrl}", httpContent);
				response.EnsureSuccessStatusCode();

				string responseContent = await response.Content.ReadAsStringAsync();

				var deserializationOptions = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				DeleteDocumentResponse? deserializedResponse = JsonSerializer.Deserialize<DeleteDocumentResponse>(responseContent, deserializationOptions);
				return deserializedResponse;
			}
			catch (Exception ex)
			{
				// Log or handle exception as needed
				return null;
			}
		}
	}
}
