using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Request DTO used to upload a document to the API Gateway.
	/// </summary>
	[DataContract]
	public sealed class DocumentUploadRequest : IJsonSerializableRequest
	{
		/// <summary>
		/// Gets or sets uploaded title.
		/// </summary>
		[JsonPropertyName("title")]
		[DataMember]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets date since when document is valid.
		/// </summary>
		[JsonPropertyName("validFrom")]
		[DataMember]
		public DateOnly ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets date till document is valid. Nullable when document has no end date.
		/// </summary>
		[JsonPropertyName("validTo")]
		[DataMember]
		public DateOnly? ValidTo { get; set; }

		/// <summary>
		/// Gets or sets document file bytes.
		/// </summary>
		[JsonPropertyName("fileBytes")]
		[DataMember]
		public byte[] FileBytes { get; set; } = Array.Empty<byte>();

		/// <summary>
		/// Document format (docx by default)
		/// </summary>
		[JsonPropertyName("format")]
		[DataMember]
		public DocumentFormat Format { get; set; } = DocumentFormat.Docx;
	}
}