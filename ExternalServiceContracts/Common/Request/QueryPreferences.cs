using System.Text;
using CommonSDK;

namespace ExternalServiceContracts.Common
{
	/// <summary>
	/// Preferences that control how the query should be processed or how answers should be returned.
	/// </summary>
	public sealed class QueryPreferences : IOptimizedStringOperations
	{
		/// <summary>
		/// Gets or sets preferred language for the answer.
		/// </summary>
		public Language Language { get; init; } = Language.RS;

		/// <summary>
		/// Gets or sets preferred answer style.
		/// </summary>
		public AnswerStyle AnswerStyle { get; init; } = AnswerStyle.ConciseWithCitations;

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
			sb.AppendLine($"Language: {Language}");
			sb.AppendLine($"Answer Style: {AnswerStyle}");
		}
	}
}