using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DocumentIndexingService.IndexingData
{
	[DataContract]
	internal sealed class DocumentSectionIndex
	{
		[DataMember]
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[DataMember]
		[JsonPropertyName("payload")]
		public DocumentSectionData Payload { get; set; }

		[DataMember]
		[JsonPropertyName("vector")]
		public float[] Vector { get; set; }
	}
}
