using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace ExternalServiceContracts.Responses
{
	/// <summary>
	/// Represents the response returned when retrieving the documents.
	/// </summary>
	public sealed class GetDocumentsResponse : ISerializableResponse
	{
		/// <summary>
		/// Gets or sets the list of document descriptors.
		/// </summary>
		[JsonPropertyName("documents")]
		public List<DocumentItemDescriptor> Documents { get; set; } = new List<DocumentItemDescriptor>();
	}
}
