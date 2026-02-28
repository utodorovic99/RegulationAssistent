namespace DocumentStorageService.Storage
{
	/// <summary>
	/// Represents the index information for a document item stored in the document storage service.
	/// </summary>
	internal sealed class DocumentItemIndex
	{
		/// <summary>
		/// Gets or sets the title of the document item.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the version number of the document item.
		/// </summary>
		public int VersionNumber { get; set; }

		/// <summary>
		/// Gets or sets the date and time when the document item becomes valid.
		/// </summary>
		public DateTime? ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets the date and time when the document item stops being valid.
		/// </summary>
		public DateTime? ValidTo { get; set; }

		/// <summary>
		/// Gets or sets the URI of the blob storage where the document item is stored.
		/// </summary>
		public string? BlobUri { get; set; }
	}
}