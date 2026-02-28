using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using DocumentStorageService.Storage;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace DocumentStorageService
{
	/// <summary>
	/// Encapsulates interactions with Service Fabric reliable collections for document metadata persistence.
	/// </summary>
	internal sealed class DocumentPersistenceStorage
	{
		private const string DocumentsMetaDictionaryName = "documents-metadata";
		private readonly IReliableStateManager stateManager;
		private BlobContainerClient blobContainerClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentPersistenceStorage"/> class.
		/// </summary>
		/// <param name="stateManager"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public DocumentPersistenceStorage(IReliableStateManager stateManager)
		{
			ArgumentNullException.ThrowIfNull(stateManager, nameof(stateManager));
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

			var blobServiceClient = new BlobServiceClient(conn);
			blobContainerClient = blobServiceClient.GetBlobContainerClient("documents");
			// Use the async API to create the container if it doesn't exist.
			await this.blobContainerClient.CreateIfNotExistsAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Stores document metadata into a reliable dictionary.
		/// </summary>
		public async Task<DocumentItemDescriptor> StoreDocumentAsync(DocumentUploadRequest request)
		{
			ArgumentNullException.ThrowIfNull(request, nameof(request));
			ArgumentNullException.ThrowIfNullOrWhiteSpace(request.Title, nameof(request.Title));

			string sanitizedTitleName = SanitizeForBlobName(request.Title);
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
					string blobName = string.IsNullOrEmpty(sanitizedTitleName) ? $"document-v{nextVersion}" : $"{sanitizedTitleName}-v{nextVersion}";

					var blobClient = this.blobContainerClient.GetBlobClient(blobName);
					using (var ms = new MemoryStream(request.FileBytes))
					{
						ms.Position = 0;
						await blobClient.UploadAsync(ms, overwrite: true).ConfigureAwait(false);
					}

					blobUri = blobClient.Uri;
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

		/// <summary>
		/// Sanitizes an arbitrary string to a blob-name friendly form. Replaces disallowed characters with '-'
		/// collapses runs of separators, trims length and ensures there are no leading/trailing separators.
		/// </summary>
		private static string SanitizeForBlobName(string input, int maxLength = 200)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return string.Empty;
			}

			string s = input.Trim();
			s = s.ToLowerInvariant();
			s = Regex.Replace(s, @"[^a-z0-9_\/\-]+", "-");
			s = Regex.Replace(s, "[-_]{2,}", "-");
			s = s.Trim('-', '_', '/');

			if (s.Length > maxLength)
			{
				s = s.Substring(0, maxLength);
			}

			return s;
		}
	}
}