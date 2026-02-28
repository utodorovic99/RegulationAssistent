using System.Collections.Generic;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	/// <summary>
	/// Response to the request to get documents.
	/// </summary>
	public sealed class GetDocumentsResponse
	{
		/// <summary>
		/// Gets or sets collection of documents as response.
		/// </summary>
		public IEnumerable<DocumentItemDescriptor> Documents { get; set; }
	}
}