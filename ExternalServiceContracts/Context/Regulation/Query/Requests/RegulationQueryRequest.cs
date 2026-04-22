using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Represents a user question or query that needs to be evaluated against regulation logic.
	/// </summary>
	[DataContract]
	public sealed class RegulationQueryRequest : IJsonSerializableRequest
	{
		/// <summary>
		/// Gets or sets the textual question to evaluate against regulation rules.
		/// </summary>
		[DataMember]
		[JsonPropertyName("question")]
		public string? Question { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets contextual information accompanying the query (user, source, metadata).
		/// </summary>
		[DataMember]
		[JsonPropertyName("context")]
		public RegulationQueryContext Context { get; set; } = default!;

		/// <summary>
		/// Performs lightweight validation of the request.
		/// </summary>
		public bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(Question)
				&& Context != null
				&& Context.IsValid();
		}
	}
}