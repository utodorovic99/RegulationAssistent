using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	/// <summary>
	/// Response containing the document file bytes.
	/// </summary>
	[DataContract]
	public sealed class GetDocumentResponse : IJsonSerializableResponse
	{
		/// <summary>
		/// Gets or sets the document file bytes.
		/// </summary>
		[JsonPropertyName("fileBytes")]
		[DataMember]
		public byte[]? FileBytes { get; set; }

		/// <summary>
		/// Gets or sets the document title.
		/// </summary>
		[JsonPropertyName("title")]
		[DataMember]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the version number.
		/// </summary>
		[JsonPropertyName("versionNumber")]
		[DataMember]
		public int VersionNumber { get; set; }

		/// <summary>
		/// Document format (docx by default)
		/// </summary>
		[JsonPropertyName("format")]
		[DataMember]
		public DocumentFormat Format { get; set; }
	}
}