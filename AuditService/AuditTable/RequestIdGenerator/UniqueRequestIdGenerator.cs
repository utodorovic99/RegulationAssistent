using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace AuditService
{
	internal sealed class UniqueRequestIdGenerator : IUniqueRequestIdGenerator
	{
		private readonly IReliableStateManager stateManager;
		public UniqueRequestIdGenerator(IReliableStateManager stateManager)
		{
			this.stateManager = stateManager;
		}

		/// <inheritdoc/>
		public long GenerateUniqueRequestId()
		{
			var dict = stateManager.GetOrAddAsync<IReliableDictionary<string, long>>("uniqueCounters").GetAwaiter().GetResult();

			using (var tx = stateManager.CreateTransaction())
			{
				var newValue = dict.AddOrUpdateAsync(tx, "uniqueUpdateCounter", 1L, (k, v) => unchecked(v + 1)).GetAwaiter().GetResult();
				tx.CommitAsync().GetAwaiter().GetResult();
				return newValue;
			}
		}
	}
}
