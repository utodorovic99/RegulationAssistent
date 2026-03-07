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
	public sealed class DocumentUploadRequest : ISerializableRequest
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
		public DateTime? ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets date till document is valid.
		/// </summary>
		[JsonPropertyName("validTo")]
		[DataMember]
		public DateTime? ValidTo { get; set; }

		/// <summary>
		/// Gets or sets document file bytes.
		/// </summary>
		[JsonPropertyName("fileBytes")]
		[DataMember]
		public byte[] FileBytes { get; set; } = Array.Empty<byte>();
	}
}