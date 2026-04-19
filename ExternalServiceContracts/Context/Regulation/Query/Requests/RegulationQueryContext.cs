using System;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Contextual information accompanying a regulation query (e.g., user, source, metadata).
	/// </summary>
	public sealed class RegulationQueryContext
	{
		/// <summary>
		/// Gets date when the query is made or applies.
		/// </summary>
		public DateOnly Date { get; init; } = default;

		/// <summary>
		/// Validates the RegulationQueryContext instance.
		/// </summary>
		public bool IsValid()
		{
			return Date == default;
		}
	}
}