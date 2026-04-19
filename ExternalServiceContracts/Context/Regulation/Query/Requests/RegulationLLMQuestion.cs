using System.Collections.Generic;
using System.Runtime.Serialization;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Embeddings;

namespace ExternalServiceContracts.Requests
{
	[DataContract]
	public sealed class RegulationLLMQuestion : IJsonSerializableRequest
	{
		[DataMember]
		public string Question { get; set; } = default!;

		[DataMember]
		public List<RelevantSection> RelevantSections { get; set; } = new List<RelevantSection>(0);

		public bool IsValid()
		{
			return !string.IsNullOrWhiteSpace(Question)
				&& RelevantSections?.Count > 0;
		}
	}
}
