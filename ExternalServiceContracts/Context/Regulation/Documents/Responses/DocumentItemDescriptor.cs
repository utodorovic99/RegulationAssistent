using System;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	/// <summary>
	/// Gets descriptor for single document.
	/// </summary>
	public sealed class DocumentItemDescriptor
	{
		/// <summary>
		/// Gets or sets document title.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets document version number.
		/// </summary>
		public int VersionNumber { get; set; }

		/// <summary>
		/// Gets or sets date from document is considered as valid.
		/// </summary>
		public DateTime? ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets date till document is considered as valid.
		/// </summary>
		public DateTime? ValidTo { get; set; }

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is DocumentItemDescriptor descriptor
				&& Title == descriptor.Title
				&& VersionNumber == descriptor.VersionNumber
				&& ValidFrom == descriptor.ValidFrom
				&& ValidTo == descriptor.ValidTo;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return HashCode.Combine(Title, VersionNumber);
		}
	}
}