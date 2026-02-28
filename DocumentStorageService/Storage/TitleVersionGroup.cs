using DocumentStorageService.Storage;

namespace DocumentStorageService
{
	/// <summary>
	/// Represent group of title versions.
	/// </summary>
	internal sealed class TitleVersionGroup
	{
		/// <summary>
		/// Gets or sets document title shared by the grouped versions.
		/// </summary>
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets descriptors for each version.
		/// </summary>
		public List<DocumentItemIndex> Versions { get; } = new List<DocumentItemIndex>();
	}
}