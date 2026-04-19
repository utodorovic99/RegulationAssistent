namespace DocumentIndexingService.Embeddings
{
	internal sealed class EmbeddingDbConfiguration
	{
		public string? Url { get; set; }
		public string? Collection { get; set; }

		public bool IsValid => !string.IsNullOrEmpty(Url)
			&& !string.IsNullOrEmpty(Collection);
	}
}