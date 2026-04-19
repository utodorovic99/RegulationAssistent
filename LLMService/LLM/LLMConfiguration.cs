namespace ResponseService
{
	internal sealed class LLMConfiguration
	{
		public string? EmbeddingsPath { get; set; }
		public string? EmbeddingsModel { get; set; }
		public string? CompletionsPath { get; set; }
		public string? CompletionsModel { get; set; }

		public bool IsValid => !string.IsNullOrEmpty(EmbeddingsPath)
			&& !string.IsNullOrEmpty(EmbeddingsModel);
	}
}
