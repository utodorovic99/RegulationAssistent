using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	[DataContract]
	public sealed class StoreDocumentResponse : IJsonSerializableResponse
	{
		[JsonPropertyName("documentDescriptor")]
		[DataMember]
		public DocumentItemDescriptor? DocumentDescriptor { get; set; }

		[JsonPropertyName("success")]
		[DataMember]
		public bool Success { get; set; }

		[JsonPropertyName("errorMessage")]
		[DataMember]
		public string? ErrorMessage { get; set; }
	}
}