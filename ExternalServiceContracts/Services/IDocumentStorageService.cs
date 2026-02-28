using System.Threading.Tasks;
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
	}
}