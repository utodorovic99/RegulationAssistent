using CommonSDK;
using ExternalServiceContracts.Common;
using System.Text;
using System.Text.Json.Serialization;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Represents a user question or query that needs to be evaluated against regulation logic.
	/// </summary>
	public sealed class RegulationQueryRequest : ISerializableRequest
	{
		/// <summary>
		/// The textual question to evaluate against regulation rules.
		/// Serialized as JSON property 'question'.
		/// </summary>
		[JsonPropertyName("question")]
		public string? Question { get; set; } = string.Empty;

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

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			AppendSelfAsString(sb);

			return sb.ToString();
		}

		/// <inheritdoc/>
		public void AppendSelfAsString(StringBuilder sb)
		{
			sb.AppendLine($"Question: {Question}");
			Context.AppendSelfAsString(sb);
			Preferences.AppendSelfAsString(sb);
		}

		/// <inheritdoc/>
		public string GetRequestIdentifier()
		{
			return Question ?? string.Empty;
		}
	}
}