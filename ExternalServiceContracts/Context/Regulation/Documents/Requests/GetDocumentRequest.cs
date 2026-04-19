using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Documents.Requests
{
	/// <summary>
	/// Request to retrieve a specific document by title and version.
	/// </summary>
	[DataContract]
	public sealed class GetDocumentRequest : IJsonSerializableRequest
	{
		/// <summary>
		/// Gets or sets the title of the document to retrieve.
		/// </summary>
		[JsonPropertyName("title")]
		[DataMember]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the version number of the document to retrieve.
		/// </summary>
		[JsonPropertyName("versionNumber")]
		[DataMember]
		public int VersionNumber { get; set; }
	}
}