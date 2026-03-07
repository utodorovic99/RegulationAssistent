using System.Runtime.Serialization;
using DocumentStorageService.Storage;

namespace DocumentStorageService
{
	/// <summary>
	/// Represent group of title versions.
	/// </summary>
	[DataContract]
	internal sealed class TitleVersionGroup
	{
		/// <summary>
		/// Gets or sets document title shared by the grouped versions.
		/// </summary>
		[DataMember]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets descriptors for each version.
		/// </summary>
		[DataMember]
		public List<DocumentItemIndex> Versions { get; set; } = new List<DocumentItemIndex>();
	}
}