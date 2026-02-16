using APIGatewayService.Context.Common;
using APIGatewayService.Context.Common.Request;
using System.Text.Json.Serialization;

namespace APIGatewayService.Context.RegulationQuery
{
	/// <summary>
	/// Represents a user question or query that needs to be evaluated against regulation logic.
	/// </summary>
	public sealed class RegulationQueryRequest : IDeserializedRequest
	{
		/// <summary>
		/// The textual question to evaluate against regulation rules.
		/// Serialized as JSON property 'question'.
		/// </summary>
		[JsonPropertyName("question")]
		public string? Question { get; set; }

		/// <summary>
		/// Contextual information accompanying the query (user, source, metadata).
		/// Serialized as JSON property 'context'.
		/// </summary>
		[JsonPropertyName("context")]
		public RegulationQueryContext Context { get; init; } = default!;

		/// <summary>
		/// Preferences that control how the query should be processed or returned.
		/// Serialized as JSON property 'preferences'.
		/// </summary>
		[JsonPropertyName("preferences")]
		public QueryPreferences Preferences { get; init; } = default!;
	}
}