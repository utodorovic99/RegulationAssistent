using System;
using System.Runtime.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	/// <summary>
	/// Gets descriptor for single document.
	/// </summary>
	[DataContract]
	public sealed class DocumentItemDescriptor
	{
		[DataMember]
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets document title.
		/// </summary>
		[DataMember]
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets document version number.
		/// </summary>
		[DataMember]
		public int VersionNumber { get; set; }

		/// <summary>
		/// Gets or sets date from document is considered as valid.
		/// </summary>
		[DataMember]
		public DateTime ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets date till document is considered as valid.
		/// </summary>
		[DataMember]
		public DateTime ValidTo { get; set; }

		/// <summary>
		/// Gets or sets document format (e.g., .docx).
		/// </summary>
		[DataMember]
		public DocumentFormat Format { get; set; }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is DocumentItemDescriptor descriptor
				&& Title == descriptor.Title
				&& VersionNumber == descriptor.VersionNumber
				&& ValidFrom == descriptor.ValidFrom
				&& ValidTo == descriptor.ValidTo
				&& Format == descriptor.Format;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return Id.GetHashCode();
		}
	}
}