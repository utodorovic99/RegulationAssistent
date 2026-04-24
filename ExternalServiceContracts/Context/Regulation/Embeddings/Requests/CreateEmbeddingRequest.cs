using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class CreateEmbeddingRequest : AudibleMessage, IJsonSerializableRequest
	{
		[JsonPropertyName("text")]
		[DataMember]
		public string Text { get; set; } = string.Empty;
	}
}
