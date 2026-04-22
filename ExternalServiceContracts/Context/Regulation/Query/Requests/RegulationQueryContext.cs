using System;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Contextual information accompanying a regulation query (e.g., user, source, metadata).
	/// </summary>
	[DataContract]
	public sealed class RegulationQueryContext
	{
		/// <summary>
		/// Gets date when the query is made or applies.
		/// </summary>
		[DataMember]
		[JsonPropertyName("date")]
		public DateTime Date { get; set; } = default;

		/// <summary>
		/// Validates the RegulationQueryContext instance.
		/// </summary>
		public bool IsValid()
		{
			return Date != default;
		}
	}
}