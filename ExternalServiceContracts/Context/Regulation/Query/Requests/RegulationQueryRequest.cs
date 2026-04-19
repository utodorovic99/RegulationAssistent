using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Represents a user question or query that needs to be evaluated against regulation logic.
	/// </summary>
	public sealed class RegulationQueryRequest : IJsonSerializableRequest
	{
		/// <summary>
		/// Gets or sets the textual question to evaluate against regulation rules.
		/// </summary>
		[JsonPropertyName("question")]
		public string? Question { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets contextual information accompanying the query (user, source, metadata).
		/// </summary>
		[JsonPropertyName("context")]
		public RegulationQueryContext Context { get; init; } = default!;

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