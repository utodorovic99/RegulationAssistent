using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace ExternalServiceContracts.Context.Regulation.Documents.Requests
{
	[DataContract]
	public sealed class BuildDocumentIndexRequest : IJsonSerializableRequest
	{
		[JsonPropertyName("documentDescriptor")]
		[DataMember]
		public DocumentItemDescriptor DocumentDescriptor { get; set; } = new DocumentItemDescriptor();

		[JsonPropertyName("fileBytes")]
		[DataMember]
		public byte[] FileBytes { get; set; } = System.Array.Empty<byte>();
	}
}