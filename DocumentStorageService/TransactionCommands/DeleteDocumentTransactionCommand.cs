using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Services;

namespace DocumentStorageService.Commands
{
	internal sealed class DeleteDocumentTransactionCommand : ITransactionCommand
	{
		private readonly IDocumentPersistenceStorage persistence;
		private readonly IRpServiceProxyPool serviceProxyPool;
		private readonly DeleteDocumentRequest request;
		private DeleteDocumentResponse result;
		private DocumentItemDescriptor? documentDescriptor;
		private byte[]? deletedFileBytes;
		private bool indexRemoved;

		public IJsonSerializableRequest? Request => request;
		public IJsonSerializableResponse? Result => result;

		public DeleteDocumentTransactionCommand(IDocumentPersistenceStorage persistence, IRpServiceProxyPool serviceProxyPool, DeleteDocumentRequest request)
		{
			ArgumentNullException.ThrowIfNull(nameof(persistence));
			ArgumentNullException.ThrowIfNull(nameof(request));

			this.persistence = persistence;
			this.serviceProxyPool = serviceProxyPool;
			this.request = request;

			result = new DeleteDocumentResponse { Success = false };
		}

		public async Task ExecuteAsync()
		{
			// Prepare descriptor
			documentDescriptor = new DocumentItemDescriptor
			{
				Title = request.Title,
				VersionNumber = request.VersionNumber
			};

			// Delete from persistence and capture file bytes/descriptor for potential rollback
			DeletedDocumentInfo deletedInfo = await persistence.DeleteDocumentAsync(request.Title, request.VersionNumber).ConfigureAwait(false);

			if (deletedInfo == null
					|| !deletedInfo.Deleted
					|| deletedInfo.Descriptor == null)
			{
				result = new DeleteDocumentResponse { Success = false, ErrorMessage = "Document not found" };
				return;
			}

			// store for rollback
			documentDescriptor = deletedInfo.Descriptor;
			deletedFileBytes = deletedInfo.FileBytes;

			// Remove from external index
			indexRemoved = await serviceProxyPool.GetProxy<IDocumentIndexWritter>()
				.RemoveDocumentIndex(documentDescriptor).ConfigureAwait(false);

			if (indexRemoved)
			{
				result = new DeleteDocumentResponse { Success = true };
			}
			else
			{
				result = new DeleteDocumentResponse { Success = false, ErrorMessage = "Index removal failed" };
			}
		}

		public Task CommitAsync()
		{
			// No-op: both storage deletion and index removal performed in ExecuteAsync
			return Task.CompletedTask;
		}

		public async Task RollbackAsync()
		{
			if (documentDescriptor == null)
			{
				return;
			}

			try
			{
				// If index was removed, attempt to restore document in persistence and rebuild index
				await persistence.RestoreDocumentAsync(documentDescriptor, deletedFileBytes).ConfigureAwait(false);
				var buildReq = new BuildDocumentIndexRequest { DocumentDescriptor = documentDescriptor, FileBytes = deletedFileBytes ?? System.Array.Empty<byte>() };

				//TODO: Implement 2 - phase deletion on server side (dirty bit) to avoid unnecessary processing.
				bool indexRebuit = await serviceProxyPool.GetProxy<IDocumentIndexWritter>()
					.BuildDocumentIndex(buildReq).ConfigureAwait(false);

				if (!indexRebuit)
				{
					System.Diagnostics.Trace.WriteLine($"Rollback failed: Index is not rebuilt.");
				}
			}
			catch (Exception ex)
			{
				// Do not throw from rollback; log and continue.
				System.Diagnostics.Trace.WriteLine($"Rollback failed: {ex}");
			}
		}
	}
}
