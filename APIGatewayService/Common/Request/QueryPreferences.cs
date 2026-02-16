namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Preferences that control how the query should be processed or how answers should be returned.
	/// </summary>
	public sealed class QueryPreferences
	{
		/// <summary>
		/// Preferred language for the answer (e.g., <see cref="SupportedLanguage.En"/>, <see cref="SupportedLanguage.RS"/>).
		/// </summary>
		public SupportedLanguage Language { get; init; } = SupportedLanguage.RS;

		/// <summary>
		/// Preferred answer style (e.g., <see cref="AnswerStyle.ConciseWithCitations"/>, <see cref="AnswerStyle.Detailed"/>, <see cref="AnswerStyle.BulletPoints"/>).
		/// </summary>
		public AnswerStyle AnswerStyle { get; init; } = AnswerStyle.ConciseWithCitations;
	}
}
