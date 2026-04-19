using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class AsyncEmbeddingCreationRequest : IJsonSerializableRequest
	{
		[DataMember]
		public string[] Ids { get; set; }

		[JsonPropertyName("texts")]
		[DataMember]
		public string[] Texts { get; set; } = System.Array.Empty<string>();
	}
}