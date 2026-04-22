using CommonSDK;
using DocumentStorageService.Storage;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace DocumentStorageService
{
	/// <summary>
	/// Extension class for <see cref="DocumentItemIndex"/>
	/// </summary>
	internal static class DocumentItemIndexExtensions
	{
		/// <summary>
		/// Converts a <see cref="DocumentItemIndex"/> to a <see cref="DocumentItemDescriptor"/>.
		/// </summary>
		/// <param name="index">Document index to convert.</param>
		/// <returns><see cref="DocumentItemDescriptor"/> corresponding to the current instance.</returns>
		public static DocumentItemDescriptor ToDescriptor(this DocumentItemIndex index)
		{
			return new DocumentItemDescriptor
			{
				Id = NamingHelper.CreateVersionedName(index.Title, index.VersionNumber),
				Title = index.Title,
				VersionNumber = index.VersionNumber,
				ValidFrom = index.ValidFrom.ToDateTime(TimeOnly.MinValue),
				// If ValidTo was not specified, fall back to ValidFrom so descriptor has a sensible value
				ValidTo = (index.ValidTo ?? index.ValidFrom).ToDateTime(TimeOnly.MinValue),
			};
		}
	}
}