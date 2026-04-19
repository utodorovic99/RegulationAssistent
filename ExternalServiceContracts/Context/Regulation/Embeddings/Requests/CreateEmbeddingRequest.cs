using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class CreateEmbeddingRequest : IJsonSerializableRequest
	{
		[JsonPropertyName("text")]
		[DataMember]
		public string Text { get; set; } = string.Empty;
	}
}
