using AuditService.AuditTable;
using AuditService.Model;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace AuditService
{
	/// <summary>
	/// Persistence wrapper that stores QueryAudit entities in a reliable dictionary keyed by long.
	/// A separate reliable dictionary is used as a counter to generate unique ids.
	/// </summary>
	public sealed class QueryAuditTable : IQueryAuditTable
	{
		private const string DictionaryName = "QueryAuditTableDictionary";
		private const string DictionaryLastRequest = "QueryAuditLastRequest";

		private readonly IReliableStateManager stateManager;
		private readonly IQueryTraceFormatter queryTraceFormatter;

		public QueryAuditTable(IReliableStateManager stateManager)
		{
			this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
			this.queryTraceFormatter = new QueryTraceFormatter();
		}

		public async Task StartQueryTrace(QueryAudit audit)
		{
			if (audit == null) throw new ArgumentNullException(nameof(audit));

			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			var dictLastRequest = await stateManager.GetOrAddAsync<IReliableDictionary<string, long>>(DictionaryLastRequest).ConfigureAwait(false);

			using (var tx = stateManager.CreateTransaction())
			{
				await dict.AddOrUpdateAsync(tx, audit.QueryId, audit, (k, v) => audit).ConfigureAwait(false);
				await dictLastRequest.AddOrUpdateAsync(tx, DictionaryLastRequest, audit.QueryId, (k, v) => audit.QueryId).ConfigureAwait(false);
				await tx.CommitAsync().ConfigureAwait(false);
			}
		}

		public async Task StartServiceTraceAsync(long requestId, ServiceTraceContext serviceTrace)
		{
			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await dict.TryGetValueAsync(tx, requestId).ConfigureAwait(false);
				if (result.HasValue)
				{
					if (result.Value.ServiceTracing.FirstOrDefault(x => x.ServiceName.Equals(serviceTrace.ServiceName, StringComparison.InvariantCultureIgnoreCase)) == null)
					{
						result.Value.ServiceTracing.Add(serviceTrace);
						await tx.CommitAsync().ConfigureAwait(false);
					}
				}
			}
		}

		public async Task LogServiceEventAsync(long requestId, string serviceName, ServiceEventTraceContext serviceEvent)
		{
			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await dict.TryGetValueAsync(tx, requestId).ConfigureAwait(false);
				if (result.HasValue)
				{
					var serviceTrace = result.Value.ServiceTracing.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
					if (serviceTrace != null)
					{
						serviceTrace.Events.Add(serviceEvent);
						await tx.CommitAsync().ConfigureAwait(false);
					}
				}
			}
		}

		public async Task CompleteServiceTraceAsync(long requestId, string serviceName, DateTime timestamp)
		{
			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await dict.TryGetValueAsync(tx, requestId).ConfigureAwait(false);
				if (result.HasValue)
				{
					var serviceTrace = result.Value.ServiceTracing.FirstOrDefault(x => x.ServiceName.Equals(serviceName, StringComparison.InvariantCultureIgnoreCase));
					if (serviceTrace != null)
					{
						serviceTrace.ExitTime = timestamp;
						await tx.CommitAsync().ConfigureAwait(false);
					}
				}
			}
		}

		public async Task EndQueryTrace(long requestId, string serviceName, QueryResponseAudit audit)
		{
			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await dict.TryGetValueAsync(tx, requestId).ConfigureAwait(false);
				if (result.HasValue)
				{
					result.Value.ResponseAudit = audit;
					await tx.CommitAsync().ConfigureAwait(false);
				}
			}
		}

		public async Task<string> TraceLastRequest()
		{
			string trace = string.Empty;

			var dict = await stateManager.GetOrAddAsync<IReliableDictionary<long, QueryAudit>>(DictionaryName).ConfigureAwait(false);
			var dictLastRequest = await stateManager.GetOrAddAsync<IReliableDictionary<string, long>>(DictionaryLastRequest).ConfigureAwait(false);

			using (var tx = stateManager.CreateTransaction())
			{
				var resultLastKey = dictLastRequest.TryGetValueAsync(tx, DictionaryLastRequest).Result;
				if (resultLastKey.HasValue)
				{
					var result = await dict.TryGetValueAsync(tx, resultLastKey.Value).ConfigureAwait(false);
					if (result.HasValue)
					{
						trace = queryTraceFormatter.Format(result.Value);
						await tx.CommitAsync().ConfigureAwait(false);
					}
				}
			}

			return trace;
		}
	}
}
