using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class SubmitAsyncEmbeddingsRequest : IJsonSerializableResponse
	{
		[JsonPropertyName("ids")]
		[DataMember]
		public string[] Ids { get; set; } = System.Array.Empty<string>();

		[JsonPropertyName("embeddings")]
		[DataMember]
		public float[][] Embeddings { get; set; } = System.Array.Empty<float[]>();
	}
}