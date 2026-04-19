using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Responses
{
	[DataContract]
	public sealed class CreateEmbeddingResponse : IJsonSerializableResponse
	{
		[JsonPropertyName("embedding")]
		[DataMember]
		public float[] Embedding { get; set; } = System.Array.Empty<float>();
	}
}
