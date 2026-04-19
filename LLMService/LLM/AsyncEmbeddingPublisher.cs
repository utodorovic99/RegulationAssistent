using System.Text.Json;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using ExternalServiceContracts.Services;
using LLMService;

namespace ResponseService
{
	internal sealed class AsyncEmbeddingPublisher
	{
		private const int MaxParallelPublishers = 1;
		private readonly IRpServiceProxyPool serviceProxyPool;
		private readonly SemaphoreSlim signal = new(0);
		private readonly SemaphoreSlim queueLock = new(1, 1);
		private readonly string queueFilePath;
		private int workersStarted = 0;

		public AsyncEmbeddingPublisher(IRpServiceProxyPool serviceProxyPool)
		{
			this.serviceProxyPool = serviceProxyPool;
			queueFilePath = Path.Combine(AppContext.BaseDirectory, "async-embedding-publisher-queue.json");
			EnsureQueueFileExists();
		}

		public void Enqueue(SubmitAsyncEmbeddingsRequest response)
		{
			if (response == null || response.Embeddings == null || response.Embeddings.Length == 0)
			{
				return;
			}

			EnsureWorkersStarted();
			_ = EnqueueInternalAsync(response, true);
		}

		private void EnsureWorkersStarted()
		{
			if (Interlocked.CompareExchange(ref workersStarted, 1, 0) != 0)
			{
				return;
			}

			int persistedItems = GetPersistedQueueCount();
			if (persistedItems > 0)
			{
				signal.Release(persistedItems);
			}

			for (int i = 0; i < MaxParallelPublishers; i++)
			{
				_ = Task.Run(PublisherLoopAsync);
			}
		}

		private async Task PublisherLoopAsync()
		{
			while (true)
			{
				await signal.WaitAsync().ConfigureAwait(false);

				var payload = await TryDequeueInternalAsync().ConfigureAwait(false);
				if (payload == null)
				{
					continue;
				}

				try
				{
					bool published = await serviceProxyPool.GetProxy<IEmbeddingAsyncHandler>()
						.ProcessEmbeddingChunkCreated(payload).ConfigureAwait(false);

					if (!published)
					{
						await Task.Delay(250).ConfigureAwait(false);
						await EnqueueInternalAsync(payload, true).ConfigureAwait(false);
					}
				}
				catch (Exception e)
				{
					ServiceEventSource.Current.Message($"AsyncEmbeddingPublisher: publish failed: {e.Message}");
					await Task.Delay(500).ConfigureAwait(false);
					await EnqueueInternalAsync(payload, true).ConfigureAwait(false);
				}
			}
		}

		private async Task EnqueueInternalAsync(SubmitAsyncEmbeddingsRequest payload, bool releaseSignal)
		{
			await queueLock.WaitAsync().ConfigureAwait(false);
			try
			{
				var items = await LoadAllAsync().ConfigureAwait(false);
				items.Add(payload);
				await SaveAllAsync(items).ConfigureAwait(false);
			}
			finally
			{
				queueLock.Release();
			}

			if (releaseSignal)
			{
				signal.Release();
			}
		}

		private async Task<SubmitAsyncEmbeddingsRequest?> TryDequeueInternalAsync()
		{
			await queueLock.WaitAsync().ConfigureAwait(false);
			try
			{
				var items = await LoadAllAsync().ConfigureAwait(false);
				if (items.Count == 0)
				{
					return null;
				}

				var first = items[0];
				items.RemoveAt(0);
				await SaveAllAsync(items).ConfigureAwait(false);
				return first;
			}
			finally
			{
				queueLock.Release();
			}
		}

		private async Task<List<SubmitAsyncEmbeddingsRequest>> LoadAllAsync()
		{
			if (!File.Exists(queueFilePath))
			{
				return new List<SubmitAsyncEmbeddingsRequest>();
			}

			string json = await File.ReadAllTextAsync(queueFilePath).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(json))
			{
				return new List<SubmitAsyncEmbeddingsRequest>();
			}

			return JsonSerializer.Deserialize<List<SubmitAsyncEmbeddingsRequest>>(json)
				?? new List<SubmitAsyncEmbeddingsRequest>();
		}

		private async Task SaveAllAsync(List<SubmitAsyncEmbeddingsRequest> items)
		{
			string json = JsonSerializer.Serialize(items);
			await File.WriteAllTextAsync(queueFilePath, json).ConfigureAwait(false);
		}

		private void EnsureQueueFileExists()
		{
			if (!File.Exists(queueFilePath))
			{
				File.WriteAllText(queueFilePath, "[]");
			}
		}

		private int GetPersistedQueueCount()
		{
			try
			{
				if (!File.Exists(queueFilePath))
				{
					return 0;
				}

				var json = File.ReadAllText(queueFilePath);
				if (string.IsNullOrWhiteSpace(json))
				{
					return 0;
				}

				var list = JsonSerializer.Deserialize<List<SubmitAsyncEmbeddingsRequest>>(json);
				return list?.Count ?? 0;
			}
			catch
			{
				return 0;
			}
		}
	}
}
