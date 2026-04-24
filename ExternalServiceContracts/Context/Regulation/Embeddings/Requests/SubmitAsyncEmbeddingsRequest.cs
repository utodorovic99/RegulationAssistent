using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class SubmitAsyncEmbeddingsRequest : AudibleMessage, IJsonSerializableResponse
	{
		[JsonPropertyName("ids")]
		[DataMember]
		public string[] Ids { get; set; } = System.Array.Empty<string>();

		[JsonPropertyName("embeddings")]
		[DataMember]
		public float[][] Embeddings { get; set; } = System.Array.Empty<float[]>();
	}
}