using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DocumentStorageService
{
	internal sealed class DeletedDocumentInfo
	{
		public bool Deleted { get; set; }
		public DocumentItemDescriptor? Descriptor { get; set; }
		public byte[]? FileBytes { get; set; }

		public static DeletedDocumentInfo NotFound => new DeletedDocumentInfo { Deleted = false };
	}

	internal interface IDocumentPersistenceStorage
	{
		Task<DeletedDocumentInfo> DeleteDocumentAsync(string title, int versionNumber);

		Task<bool> RestoreDocumentAsync(DocumentItemDescriptor descriptor, byte[]? fileBytes);

		Task<List<DocumentItemDescriptor>> GetAllDocumentsAsync();

		Task<DocumentItemDescriptor?> GetLatestDocumentByTitleAsync(string title);

		Task<byte[]?> GetDocumentBytesAsync(string title, int versionNumber);

		Task InitializeAsync();

		Task<DocumentItemDescriptor> StoreDocumentAsync(DocumentUploadRequest request);
	}
}