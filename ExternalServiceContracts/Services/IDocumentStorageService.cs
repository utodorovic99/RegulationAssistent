using System.Collections.Generic;
using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	/// <summary>
	/// Defines a contract for a document storage service that allows storing documents and their metadata.
	/// </summary>
	public interface IDocumentStorageService : IService
	{
		/// <summary>
		/// Store a document into a reliable dictionary. Delegates to DocumentPersistenceStorage.
		/// </summary>
		/// <param name="request">The document upload request containing the document metadata.</param>
		Task<DocumentItemDescriptor> StoreDocument(DocumentUploadRequest request);

		/// <summary>
		/// Retrieves all stored documents.
		/// </summary>
		/// <returns>List of all document descriptors.</returns>
		Task<List<DocumentItemDescriptor>> GetAllDocuments();

		/// <summary>
		/// Retrieves a specific document's bytes by title and version number.
		/// </summary>
		/// <param name="request">Request containing the document title and version number.</param>
		/// <returns>Response containing the document bytes.</returns>
		Task<GetDocumentResponse?> GetDocument(GetDocumentRequest request);

		/// <summary>
		/// Deletes a specific document by title and version number.
		/// </summary>
		/// <param name="request">Request containing the document title and version number to delete.</param>
		/// <returns>Response indicating success or failure.</returns>
		Task<DeleteDocumentResponse> DeleteDocument(DeleteDocumentRequest request);
	}
}