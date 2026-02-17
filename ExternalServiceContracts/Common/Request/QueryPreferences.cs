using CommonSDK;
using System.Text;

namespace ExternalServiceContracts.Common
{
	/// <summary>
	/// Preferences that control how the query should be processed or how answers should be returned.
	/// </summary>
	public sealed class QueryPreferences : IOptimizedStringOperations
	{
		/// <summary>
		/// Preferred language for the answer (e.g., <see cref="Language.En"/>, <see cref="Language.RS"/>).
		/// </summary>
		public Language Language { get; init; } = Language.RS;

		/// <summary>
		/// Preferred answer style (e.g., <see cref="AnswerStyle.ConciseWithCitations"/>, <see cref="AnswerStyle.Detailed"/>, <see cref="AnswerStyle.BulletPoints"/>).
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