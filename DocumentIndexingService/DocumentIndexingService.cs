using System.Fabric;
using CommonSDK.ServiceProxies;
using DocumentIndexingService.Embeddings;
using DocumentIndexingService.IndexingData;
using DocumentIndexingService.Parsers;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace DocumentIndexingService
{
	internal sealed class DocumentIndexingService : StatefulService, IDocumentIndexWritter, IDocumentIndexReader, IEmbeddingAsyncHandler
	{
		private const string PendingSectionIndicesDictionaryName = "pendingSectionIndices";
		private readonly IEmbeddingDb embeddingDb;

		private readonly IRpServiceProxyPool serviceProxyPool;
		private readonly IDictionary<CommonSDK.DocumentFormat, IDocumentParser> documentParsers;

		public DocumentIndexingService(StatefulServiceContext context) : base(context)
		{
			serviceProxyPool = new RpServiceProxyPool();
			serviceProxyPool.RegisterFabricRP2Proxy<ILLMService>("fabric:/RegulationAssistent/LLMService", ServiceType.Stateful);

			documentParsers = new Dictionary<CommonSDK.DocumentFormat, IDocumentParser>(1);
			documentParsers.Add(CommonSDK.DocumentFormat.Docx, new WordDocumentParser());

			embeddingDb = new QdrantEmbeddingDb();
		}

		public async Task<bool> BuildDocumentIndex(BuildDocumentIndexRequest request)
		{
			if (request == null
					|| request.DocumentDescriptor == null
					|| request.FileBytes == null
					|| request.FileBytes.Length == 0)
			{
				return false;
			}

			try
			{
				if (!documentParsers.TryGetValue(request.DocumentDescriptor.Format, out IDocumentParser documentParser))
				{
					return false;
				}

				List<DocumentSectionIndex> sectionIndices = documentParser.ParseSections(request.FileBytes, request.DocumentDescriptor);
				if (sectionIndices.Count == 0)
				{
					return true;
				}

				var pendingDictionary = await StateManager
					.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, DocumentSectionIndex>>(PendingSectionIndicesDictionaryName)
					.ConfigureAwait(false);

				using (var tx = StateManager.CreateTransaction())
				{
					foreach (var section in sectionIndices)
					{
						if (section == null || string.IsNullOrWhiteSpace(section.Id))
						{
							continue;
						}

						await pendingDictionary.SetAsync(tx, section.Id, section).ConfigureAwait(false);
					}

					await tx.CommitAsync().ConfigureAwait(false);
				}

				AsyncEmbeddingCreationRequest createEmbeddingBulkRequest = new AsyncEmbeddingCreationRequest
				{
					Ids = sectionIndices.Select(x => x.Id).ToArray(),
					Texts = sectionIndices.Select(x => x.Payload.Text).ToArray(),
				};

				bool submitSucceeded = await serviceProxyPool.GetProxy<ILLMService>()
					.SubmitEmbeddingCreationBulkRequest(createEmbeddingBulkRequest).ConfigureAwait(false);

				if (!submitSucceeded)
				{
					using var rollbackTx = StateManager.CreateTransaction();
					foreach (var id in createEmbeddingBulkRequest.Ids)
					{
						if (!string.IsNullOrWhiteSpace(id))
						{
							await pendingDictionary.TryRemoveAsync(rollbackTx, id).ConfigureAwait(false);
						}
					}
					await rollbackTx.CommitAsync().ConfigureAwait(false);
				}

				return submitSucceeded;
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"BuildDocumentIndex failed: {ex.Message}");
				return false;
			}
		}

		public async Task<bool> RemoveDocumentIndex(DocumentItemDescriptor document)
		{
			if (document == null) return false;

			try
			{
				await embeddingDb.DeleteIndices(document.Id).ConfigureAwait(false);
				return true;
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to delete indices from Qdrant for document {document.Id}: {e.Message}");
			}

			return false;
		}

		public async Task<bool> ProcessEmbeddingChunkCreated(SubmitAsyncEmbeddingsRequest response)
		{
			if (response?.Ids == null || response.Embeddings == null || response.Ids.Length == 0 || response.Embeddings.Length == 0)
			{
				return false;
			}

			if (response.Ids.Length != response.Embeddings.Length)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"ProcessEmbeddingChunkCreated rejected: ids/embeddings length mismatch. Ids={response.Ids.Length}, Embeddings={response.Embeddings.Length}");
				return false;
			}

			try
			{
				var pendingDictionary = await StateManager
					.GetOrAddAsync<Microsoft.ServiceFabric.Data.Collections.IReliableDictionary<string, DocumentSectionIndex>>(PendingSectionIndicesDictionaryName)
					.ConfigureAwait(false);

				var completed = new List<DocumentSectionIndex>(response.Ids.Length);
				using (var tx = StateManager.CreateTransaction())
				{
					for (int i = 0; i < response.Ids.Length; i++)
					{
						string id = response.Ids[i];
						if (string.IsNullOrWhiteSpace(id))
						{
							continue;
						}

						var existing = await pendingDictionary.TryGetValueAsync(tx, id).ConfigureAwait(false);
						if (!existing.HasValue || existing.Value == null)
						{
							continue;
						}

						existing.Value.Vector = response.Embeddings[i] ?? Array.Empty<float>();
						completed.Add(existing.Value);
						await pendingDictionary.TryRemoveAsync(tx, id).ConfigureAwait(false);
					}

					await tx.CommitAsync().ConfigureAwait(false);
				}

				if (completed.Count > 0)
				{
					await embeddingDb.StoreIndicesAsync(completed).ConfigureAwait(false);
				}

				return true;
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"ProcessEmbeddingChunkCreated failed: {e.Message}");
				return false;
			}
		}

		public async Task<GetRelevantSectionsResponse> GetIndexedResults(GetRelevantSectionsRequest request)
		{
			if (request != null)
			{

				try
				{
					await embeddingDb.GetIndexedResults(request).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to retrieve indexed results from Qdrant for document: {e.Message}");
				}
			}

			return GetRelevantSectionsResponse.EmptyResponse;
		}

		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new ServiceReplicaListener[1]
			{
				new ServiceReplicaListener(ctx =>
					new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}
	}
}
