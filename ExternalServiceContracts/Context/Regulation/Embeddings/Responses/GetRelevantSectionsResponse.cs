using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Responses
{
	[DataContract]
	public class GetRelevantSectionsResponse
	{
		public static readonly GetRelevantSectionsResponse EmptyResponse = new GetRelevantSectionsResponse
		{
			RelevantSections = new List<RelevantSection>(0)
		};

		[DataMember]
		public List<RelevantSection> RelevantSections { get; set; } = new List<RelevantSection>(0);
	}
}
