using CommonSDK;
using DocumentStorageService.Storage;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DocumentStorageService
{
	/// <summary>
	/// Encapsulates interactions with Service Fabric reliable collections for document metadata persistence.
	/// </summary>
	internal sealed class DocumentPersistenceStorage : IDocumentPersistenceStorage
	{
		private const string DocumentsMetaDictionaryName = "documents-metadata";
		private readonly IReliableStateManager stateManager;
		private CloudBlobContainer? blobContainer;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentPersistenceStorage"/> class.
		/// </summary>
		/// <param name="stateManager"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public DocumentPersistenceStorage(IReliableStateManager stateManager)
		{
			ArgumentNullException.ThrowIfNull(stateManager, nameof(stateManager));
			this.stateManager = stateManager;
		}

		/// <summary>
		/// Initializes the blob container client for Azure Blob Storage interactions.
		/// </summary>
		/// <returns>Task representing initialization routine.</returns>
		/// <exception cref="InvalidOperationException">If blob initialization is attempted with invalid parameters.</exception>
		public async Task InitializeAsync()
		{
			// Initialize Azure Blob container client using connection string from environment variable.
			string? conn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
			if (string.IsNullOrWhiteSpace(conn))
			{
				throw new InvalidOperationException("Missing environment variable 'AZURE_STORAGE_CONNECTION_STRING' required for Azure Blob Storage integration.");
			}

			CloudStorageAccount storageAccount = CloudStorageAccount.Parse(conn);
			CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
			blobContainer = blobClient.GetContainerReference("documents");
			await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Stores document metadata into a reliable dictionary.
		/// </summary>
		public async Task<DocumentItemDescriptor> StoreDocumentAsync(DocumentUploadRequest request)
		{
			ArgumentNullException.ThrowIfNull(request, nameof(request));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(request.Title, nameof(request.Title));

			if (blobContainer == null)
			{
				throw new InvalidOperationException("Blob container is not initialized. Ensure InitializeAsync() has been called and completed successfully.");
			}

			string sanitizedTitleName = NamingHelper.SanitizeName(request.Title);
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName).ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				// Try get existing group
				var existingGroup = await groupsDict.TryGetValueAsync(tx, sanitizedTitleName).ConfigureAwait(false);

				TitleVersionGroup group;
				int nextVersion;

				if (!IsAddingFirstItemInTheGroup(existingGroup))
				{
					nextVersion = existingGroup.Value.Versions.Last().VersionNumber + 1;
					group = existingGroup.Value;
				}
				else
				{
					group = new TitleVersionGroup { Title = request.Title };
					nextVersion = 1;
				}

				Uri? blobUri = null;
				if (request.FileBytes != null && request.FileBytes.Length > 0)
				{
					string blobName = NamingHelper.CreateVersionedName(sanitizedTitleName, nextVersion, sanitizeName: false);

					CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);
					using (var ms = new MemoryStream(request.FileBytes))
					{
						ms.Position = 0;
						await blockBlob.UploadFromStreamAsync(ms).ConfigureAwait(false);
					}

					blobUri = blockBlob.Uri;
				}

				var latestTitleVersion = new DocumentItemIndex
				{
					Title = request.Title,
					VersionNumber = nextVersion,
					ValidFrom = request.ValidFrom,
					ValidTo = request.ValidTo,
					BlobUri = blobUri?.ToString(),
				};

				group.Versions.Add(latestTitleVersion);
				await groupsDict.AddOrUpdateAsync(tx, sanitizedTitleName, group, (k, existing) => group).ConfigureAwait(false);
				await tx.CommitAsync().ConfigureAwait(false);

				return latestTitleVersion.ToDescriptor();
			}
		}

		/// <summary>
		/// Retrieves all document descriptors from the reliable dictionary.
		/// </summary>
		/// <returns>List of all document descriptors.</returns>
		public async Task<List<DocumentItemDescriptor>> GetAllDocumentsAsync()
		{
			var result = new List<DocumentItemDescriptor>();
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName).ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				var enumerable = await groupsDict.CreateEnumerableAsync(tx).ConfigureAwait(false);
				var enumerator = enumerable.GetAsyncEnumerator();

				while (await enumerator.MoveNextAsync(CancellationToken.None).ConfigureAwait(false))
				{
					var group = enumerator.Current.Value;
					if (group?.Versions != null)
					{
						foreach (var version in group.Versions)
						{
							result.Add(version.ToDescriptor());
						}
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Retrieves the latest document descriptor for the given title, or null if none exists.
		/// </summary>
		public async Task<DocumentItemDescriptor?> GetLatestDocumentByTitleAsync(string title)
		{
			if (string.IsNullOrWhiteSpace(title)) return null;

			string sanitizedTitleName = NamingHelper.SanitizeName(title);
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName). ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				var existingGroup = await groupsDict.TryGetValueAsync(tx, sanitizedTitleName).ConfigureAwait(false);

				if (!existingGroup.HasValue || existingGroup.Value?.Versions == null || existingGroup.Value.Versions.Count == 0)
				{
					return null;
				}

				var latest = existingGroup.Value.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
				return latest?.ToDescriptor();
			}
		}

		/// <summary>
		/// Retrieves document bytes from Azure Blob Storage by title and version number.
		/// </summary>
		/// <param name="title">The title of the document to retrieve.</param>
		/// <param name="versionNumber">The version number of the document to retrieve.</param>
		/// <returns>Byte array containing the document content, or null if not found.</returns>
		public async Task<byte[]?> GetDocumentBytesAsync(string title, int versionNumber)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(title, nameof(title));

			if (blobContainer == null)
			{
				throw new InvalidOperationException("Blob container is not initialized. Ensure InitializeAsync() has been called and completed successfully.");
			}

			string sanitizedTitleName = NamingHelper.SanitizeName(title);
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName).ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				var existingGroup = await groupsDict.TryGetValueAsync(tx, sanitizedTitleName).ConfigureAwait(false);

				if (!existingGroup.HasValue)
				{
					// Log for debugging
					System.Diagnostics.Debug.WriteLine($"Document group not found for sanitized title: '{sanitizedTitleName}' (original: '{title}')");
					return null;
				}

				var version = existingGroup.Value.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
				if (version?.BlobUri == null)
				{
					// Log for debugging
					System.Diagnostics.Debug.WriteLine($"Version {versionNumber} not found for document '{title}'");
					return null;
				}

				// Download from blob storage
				string blobName = NamingHelper.CreateVersionedName(sanitizedTitleName, versionNumber, sanitizeName: false);
				CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);

				if (!await blockBlob.ExistsAsync().ConfigureAwait(false))
				{
					// Log for debugging
					System.Diagnostics.Debug.WriteLine($"Blob not found: '{blobName}'");
					return null;
				}

				using (var ms = new MemoryStream())
				{
					await blockBlob.DownloadToStreamAsync(ms).ConfigureAwait(false);
					return ms.ToArray();
				}
			}
		}

		/// <summary>
		/// Deletes a document from both the reliable dictionary and Azure Blob Storage.
		/// </summary>
		/// <param name="title">The title of the document to delete.</param>
		/// <param name="versionNumber">The version number of the document to delete.</param>
		/// <returns>True if the document was successfully deleted; otherwise false.</returns>
		public async Task<DeletedDocumentInfo> DeleteDocumentAsync(string title, int versionNumber)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(title, nameof(title));

			if (blobContainer == null)
			{
				throw new InvalidOperationException("Blob container is not initialized. Ensure InitializeAsync() has been called and completed successfully.");
			}

			string sanitizedTitleName = NamingHelper.SanitizeName(title);
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName).ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				var existingGroup = await groupsDict.TryGetValueAsync(tx, sanitizedTitleName).ConfigureAwait(false);

				if (!existingGroup.HasValue)
				{
					return DeletedDocumentInfo.NotFound;
				}

				var group = existingGroup.Value;
				var version = group.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);

				if (version == null)
				{
					return DeletedDocumentInfo.NotFound;
				}

				byte[]? blobBytes = null;
				// Read blob content before deleting so it can be restored if needed
				if (!string.IsNullOrEmpty(version.BlobUri))
				{
					string blobName = NamingHelper.CreateVersionedName(sanitizedTitleName, versionNumber, sanitizeName: false);
					CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);

					if (await blockBlob.ExistsAsync().ConfigureAwait(false))
					{
						using (var ms = new MemoryStream())
						{
							await blockBlob.DownloadToStreamAsync(ms).ConfigureAwait(false);
							blobBytes = ms.ToArray();
						}

						// delete blob
						await blockBlob.DeleteAsync().ConfigureAwait(false);
					}
				}

				// Remove version from the group
				group.Versions.Remove(version);

				// If no versions left, remove the entire group
				if (group.Versions.Count == 0)
				{
					await groupsDict.TryRemoveAsync(tx, sanitizedTitleName).ConfigureAwait(false);
				}
				else
				{
					// Update the group with the version removed
					await groupsDict.SetAsync(tx, sanitizedTitleName, group).ConfigureAwait(false);
				}

				await tx.CommitAsync().ConfigureAwait(false);

				return new DeletedDocumentInfo { Deleted = true, Descriptor = version.ToDescriptor(), FileBytes = blobBytes };
			}
		}

		/// <summary>
		/// Restores a previously deleted document by re-adding its metadata and blob content.
		/// </summary>
		/// <param name="descriptor">The document descriptor containing metadata for the document to restore.</param>
		/// <param name="fileBytes">The blob content to restore, if available.</param>
		/// <returns>Task representing the asynchronous operation.</returns>
		public async Task<bool> RestoreDocumentAsync(DocumentItemDescriptor descriptor, byte[]? fileBytes)
		{
			if (descriptor == null) return false;

			if (blobContainer == null)
			{
				throw new InvalidOperationException("Blob container is not initialized. Ensure InitializeAsync() has been called and completed successfully.");
			}

			string title = descriptor.Title;
			int versionNumber = descriptor.VersionNumber;
			string sanitizedTitleName = NamingHelper.SanitizeName(title);
			var groupsDict = await this.stateManager.GetOrAddAsync<IReliableDictionary<string, TitleVersionGroup>>(DocumentsMetaDictionaryName).ConfigureAwait(false);

			using (var tx = this.stateManager.CreateTransaction())
			{
				var existingGroup = await groupsDict.TryGetValueAsync(tx, sanitizedTitleName).ConfigureAwait(false);

				TitleVersionGroup group;
				if (IsAddingFirstItemInTheGroup(existingGroup))
				{
					group = new TitleVersionGroup { Title = title };
				}
				else
				{
					group = existingGroup.Value;
				}

				// Upload blob content if provided
				string blobName = NamingHelper.CreateVersionedName(sanitizedTitleName, versionNumber, sanitizeName: false);
				if (fileBytes != null && fileBytes.Length > 0)
				{
					CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(blobName);
					using (var ms = new MemoryStream(fileBytes))
					{
						ms.Position = 0;
						await blockBlob.UploadFromStreamAsync(ms).ConfigureAwait(false);
					}
					string blobUri = blockBlob.Uri.ToString();

					var restoredVersion = new DocumentItemIndex
					{
						Title = title,
						VersionNumber = versionNumber,
						ValidFrom = descriptor.ValidFrom,
						ValidTo = descriptor.ValidTo,
						BlobUri = blobUri,
					};

					// Add version back (keeping version number as provided)
					group.Versions.Add(restoredVersion);
					await groupsDict.AddOrUpdateAsync(tx, sanitizedTitleName, group, (k, existing) => group).ConfigureAwait(false);
					await tx.CommitAsync().ConfigureAwait(false);
					return true;
				}

				// If there was no blob content to restore, still restore metadata entry with no blob uri
				var restoredVersionNoBlob = new DocumentItemIndex
				{
					Title = title,
					VersionNumber = versionNumber,
					ValidFrom = descriptor.ValidFrom,
					ValidTo = descriptor.ValidTo,
					BlobUri = null,
				};

				group.Versions.Add(restoredVersionNoBlob);
				await groupsDict.AddOrUpdateAsync(tx, sanitizedTitleName, group, (k, existing) => group).ConfigureAwait(false);
				await tx.CommitAsync().ConfigureAwait(false);
				return true;
			}
		}

		/// <summary>
		/// Returns true when the existingResult indicates no group exists or the group's Versions collection is null/empty.
		/// This means the incoming document will be the first item in the group.
		/// </summary>
		private static bool IsAddingFirstItemInTheGroup(ConditionalValue<TitleVersionGroup> existingResult)
		{
			if (!existingResult.HasValue)
			{
				return true;
			}

			var group = existingResult.Value;

			return group?.Versions == null
				|| group.Versions.Count == 0;
		}
	}
}