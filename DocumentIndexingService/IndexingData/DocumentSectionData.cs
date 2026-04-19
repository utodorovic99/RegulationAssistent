using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DocumentIndexingService.IndexingData
{
	[DataContract]
	internal sealed class DocumentSectionData
	{
		[DataMember]
		[JsonPropertyName("documentId")]
		public string? DocumentId { get; set; }

		[DataMember]
		[JsonPropertyName("law")]
		public string Law { get; set; }

		[DataMember]
		[JsonPropertyName("chapter")]
		public uint Chapter { get; set; }

		[DataMember]
		[JsonPropertyName("article")]
		public uint Article { get; set; }

		[DataMember]
		[JsonPropertyName("validFrom")]
		public DateOnly ValidFrom { get; set; }

		[DataMember]
		[JsonPropertyName("validTo")]
		public DateOnly ValidTo { get; set; }

		[DataMember]
		[JsonPropertyName("text")]
		public string Text { get; set; } = string.Empty;
	}
}
