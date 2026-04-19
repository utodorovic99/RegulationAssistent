using System.Runtime.Serialization;

namespace DocumentStorageService.Storage
{
	/// <summary>
	/// Represents the index information for a document item stored in the document storage service.
	/// </summary>
	[DataContract]
	internal sealed class DocumentItemIndex
	{
		/// <summary>
		/// Gets or sets the title of the document item.
		/// </summary>
		[DataMember]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the version number of the document item.
		/// </summary>
		[DataMember]
		public int VersionNumber { get; set; }

		/// <summary>
		/// Gets or sets the date when the document item becomes valid.
		/// </summary>
		[DataMember]
		public DateOnly ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets the date when the document item stops being valid. Nullable when no explicit end date is specified.
		/// </summary>
		[DataMember]
		public DateOnly? ValidTo { get; set; }

		/// <summary>
		/// Gets or sets the URI of the blob storage where the document item is stored.
		/// </summary>
		[DataMember]
		public string? BlobUri { get; set; }
	}
}