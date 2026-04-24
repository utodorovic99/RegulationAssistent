using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Requests
{
	[DataContract]
	public sealed class AsyncEmbeddingCreationRequest : AudibleMessage, IJsonSerializableRequest
	{
		[DataMember]
		public string[] Ids { get; set; }

		[JsonPropertyName("texts")]
		[DataMember]
		public string[] Texts { get; set; } = System.Array.Empty<string>();
	}
}