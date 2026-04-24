using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Responses
{
	[DataContract]
	public sealed class CreateEmbeddingResponse : AudibleMessage, IJsonSerializableResponse
	{
		[JsonPropertyName("embedding")]
		[DataMember]
		public float[] Embedding { get; set; } = System.Array.Empty<float>();
	}
}
