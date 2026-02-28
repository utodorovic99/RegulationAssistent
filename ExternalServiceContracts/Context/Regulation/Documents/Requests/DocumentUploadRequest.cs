using System;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Request DTO used to upload a document to the API Gateway.
	/// </summary>
	public sealed class DocumentUploadRequest : ISerializableRequest
	{
		/// <summary>
		/// Gets or sets uploaded title.
		/// </summary>
		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets date since when document is valid.
		/// </summary>
		[JsonPropertyName("validFrom")]
		public DateTime? ValidFrom { get; set; }

		/// <summary>
		/// Gets or sets date till document is valid.
		/// </summary>
		[JsonPropertyName("validTo")]
		public DateTime? ValidTo { get; set; }

		/// <summary>
		/// Gets or sets document file bytes.
		/// </summary>
		[JsonPropertyName("fileBytes")]
		public byte[] FileBytes { get; set; } = Array.Empty<byte>();
	}
}