using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;

namespace DocumentStorageService.Commands
{
	internal sealed class StoreDocumentTransactionCommand : ITransactionCommand
	{
		private readonly IDocumentPersistenceStorage persistence;
		private readonly IRpServiceProxyPool serviceProxyPool;
		private readonly DocumentUploadRequest request;
		private StoreDocumentResponse result;
		private DocumentItemDescriptor? storedDocument;
		private bool indexBuilt;

		public IJsonSerializableRequest? Request => request;
		public IJsonSerializableResponse? Result => result;

		public StoreDocumentTransactionCommand(IDocumentPersistenceStorage persistence, IRpServiceProxyPool serviceProxyPool, DocumentUploadRequest request)
		{
			ArgumentNullException.ThrowIfNull(nameof(persistence));
			ArgumentNullException.ThrowIfNull(nameof(serviceProxyPool));
			ArgumentNullException.ThrowIfNull(nameof(request));

			this.persistence = persistence;
			this.serviceProxyPool = serviceProxyPool;
			this.request = request;

			result = new StoreDocumentResponse { Success = false };
		}

		public async Task ExecuteAsync()
		{
			storedDocument = await persistence.StoreDocumentAsync(request).ConfigureAwait(false);

			if (storedDocument == null)
			{
				result = new StoreDocumentResponse { Success = false, ErrorMessage = "Failed to store document." };
				return;
			}

			try
			{
				var buildReq = new BuildDocumentIndexRequest
				{
					DocumentDescriptor = storedDocument,
					FileBytes = request.FileBytes ?? Array.Empty<byte>()
				};

				indexBuilt = await serviceProxyPool.GetProxy<IDocumentIndexWritter>()
					.BuildDocumentIndex(buildReq).ConfigureAwait(false);

				if (indexBuilt)
				{
					result = new StoreDocumentResponse { DocumentDescriptor = storedDocument, Success = true };
					return;
				}
			}
			catch (Exception ex)
			{
				indexBuilt = false;
				System.Diagnostics.Trace.WriteLine($"Index build failed: {ex}");
			}

			result = new StoreDocumentResponse { Success = false, ErrorMessage = $"Indexing failed" };
		}

		public Task CommitAsync()
		{
			// No-op: persistence store and index build are performed in ExecuteAsync
			return Task.CompletedTask;
		}
		public async Task RollbackAsync()
		{
			if (storedDocument == null)
			{
				return;
			}

			try
			{
				if (indexBuilt)
				{
					try
					{
						await serviceProxyPool.GetProxy<IDocumentIndexWritter>()
							.RemoveDocumentIndex(storedDocument).ConfigureAwait(false);
					}
					catch (Exception ex)
					{
						System.Diagnostics.Trace.WriteLine($"Rollback index removal failed: {ex}");
					}
				}

				await persistence.DeleteDocumentAsync(storedDocument.Title, storedDocument.VersionNumber).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				// Do not throw from rollback; log and continue.
				System.Diagnostics.Trace.WriteLine($"Rollback failed: {ex}");
			}
		}
	}
}
