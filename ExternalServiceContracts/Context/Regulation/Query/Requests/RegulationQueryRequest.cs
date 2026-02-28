using System.Text;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Common;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Represents a user question or query that needs to be evaluated against regulation logic.
	/// </summary>
	public sealed class RegulationQueryRequest : ISerializableRequest
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
		/// Gets or sets preferences that control how the query should be processed or returned.
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
	}
}