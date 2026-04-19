using System.Runtime.Serialization;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using LLMService;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace ResponseService
{
	internal sealed class AsyncEmbeddingCreator
	{
		[DataContract]
		private sealed class EmbeddingChunk
		{
			[DataMember]
			public ulong ChunkId { get; set; }

			[DataMember]
			public string[] Ids { get; set; } = Array.Empty<string>();

			[DataMember]
			public string[] Texts { get; set; } = Array.Empty<string>();

			[DataMember]
			public float[][] Embeddings { get; set; } = Array.Empty<float[]>();

			public void SetEmmbedingsAndClearTexts(float[][] embeddings)
			{
				Embeddings = embeddings;
				Texts = Array.Empty<string>(); // Clear texts to reduce memory usage
			}
		}

		private ulong chunkId = 0;
		private int workersStarted = 0;
		private readonly SemaphoreSlim workSignal = new(0);

		private readonly IReliableStateManager stateManager;
		private readonly AsyncEmbeddingPublisher embeddingPublisher;
		private readonly ILLMAgent llmAgent;
		private const string WorkQueueName = "asyncIndexWorkQueue";
		private const int DefaultChunkSize = 5;
		private const int MaxParallelEmbeddingRequests = 2;

		public AsyncEmbeddingCreator(IReliableStateManager stateManager, AsyncEmbeddingPublisher embeddingPublisher, ILLMAgent llmAgent)
		{
			this.stateManager = stateManager;
			this.embeddingPublisher = embeddingPublisher;
			this.llmAgent = llmAgent;
		}

		public async Task ProcessAsync(AsyncEmbeddingCreationRequest request, CancellationToken cancellationToken = default)
		{
			if (request?.Texts == null || request.Texts.Length == 0)
			{
				return;
			}

			EnsureWorkersStarted();

			int enqueuedCount = await EnqueueChunksAsync(request, DefaultChunkSize).ConfigureAwait(false);
			if (enqueuedCount > 0)
			{
				workSignal.Release(enqueuedCount);
			}
		}

		private void EnsureWorkersStarted()
		{
			if (Interlocked.CompareExchange(ref workersStarted, 1, 0) != 0)
			{
				return;
			}

			for (int i = 0; i < MaxParallelEmbeddingRequests; i++)
			{
				_ = Task.Run(WorkerLoopAsync);
			}
		}

		private async Task WorkerLoopAsync()
		{
			while (true)
			{
				await workSignal.WaitAsync().ConfigureAwait(false);

				var dequeued = await TryDequeueChunkAsync().ConfigureAwait(false);
				if (dequeued == null)
				{
					continue;
				}

				var (key, chunk) = dequeued.Value;
				await ProcessSingleChunkAsync(key, chunk).ConfigureAwait(false);

				embeddingPublisher.Enqueue(new SubmitAsyncEmbeddingsRequest()
				{
					Ids = chunk.Ids,
					Embeddings = chunk.Embeddings,
				});
			}
		}

		private async Task<int> EnqueueChunksAsync(AsyncEmbeddingCreationRequest request, int chunkSize)
		{
			var workQueue = await stateManager.GetOrAddAsync<IReliableDictionary<ulong, EmbeddingChunk>>(WorkQueueName).ConfigureAwait(false);
			int count = 0;

			using var tx = stateManager.CreateTransaction();
			for (int offset = 0; offset < request.Ids.Length; offset += chunkSize)
			{
				var chunkTexts = request.Texts.Skip(offset).Take(chunkSize).Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
				if (chunkTexts.Length == 0)
				{
					continue;
				}

				var chunkIds = request.Ids.Skip(offset).Take(chunkSize).Take(chunkTexts.Length).ToArray();
				var chunk = new EmbeddingChunk
				{
					ChunkId = GenerateUniqueChunkId(),
					Ids = chunkIds,
					Texts = chunkTexts,
				};

				await workQueue.SetAsync(tx, chunk.ChunkId, chunk).ConfigureAwait(false);
				count++;
			}

			await tx.CommitAsync().ConfigureAwait(false);
			return count;
		}

		private async Task<(ulong Key, EmbeddingChunk Chunk)?> TryDequeueChunkAsync()
		{
			var workQueue = await stateManager.GetOrAddAsync<IReliableDictionary<ulong, EmbeddingChunk>>(WorkQueueName).ConfigureAwait(false);

			using var tx = stateManager.CreateTransaction();
			var enumerable = await workQueue.CreateEnumerableAsync(tx).ConfigureAwait(false);
			var enumerator = enumerable.GetAsyncEnumerator();
			if (!await enumerator.MoveNextAsync(CancellationToken.None).ConfigureAwait(false))
			{
				return null;
			}

			var current = enumerator.Current;
			var removed = await workQueue.TryRemoveAsync(tx, current.Key).ConfigureAwait(false);
			if (!removed.HasValue)
			{
				return null;
			}

			await tx.CommitAsync().ConfigureAwait(false);
			return (current.Key, current.Value);
		}

		private async Task ProcessSingleChunkAsync(ulong key, EmbeddingChunk chunk)
		{
			try
			{
				var embeddings = await llmAgent.CreateEmbeddingsAsync(chunk.Texts).ConfigureAwait(false);
				if (embeddings.Length == chunk.Ids.Length)
				{
					chunk.SetEmmbedingsAndClearTexts(embeddings);
					return;
				}
				else
				{
					ServiceEventSource.Current.Message($"AsyncEmbeddingCreator: Chunk '{key}' ids/texts mismatch after embedding creation. Ids={chunk.Ids.Length}, Embeddings={embeddings.Length}");
				}
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.Message($"AsyncEmbeddingCreator: Embedding creation failed for chunk '{key}': {e.Message}");
			}

			await RequeueChunkAsync(chunk).ConfigureAwait(false);
			workSignal.Release();
		}

		private async Task RequeueChunkAsync(EmbeddingChunk chunk)
		{
			var workQueue = await stateManager.GetOrAddAsync<IReliableDictionary<ulong, EmbeddingChunk>>(WorkQueueName).ConfigureAwait(false);
			using var tx = stateManager.CreateTransaction();
			await workQueue.SetAsync(tx, chunk.ChunkId, chunk).ConfigureAwait(false);
			await tx.CommitAsync().ConfigureAwait(false);
		}

		private ulong GenerateUniqueChunkId()
		{
			return unchecked(Interlocked.Increment(ref chunkId));
		}
	}
}
