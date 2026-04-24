using System.Collections.Generic;
using System.Runtime.Serialization;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Context.Regulation.Embeddings.Responses
{
	[DataContract]
	public class GetRelevantSectionsResponse : AudibleMessage
	{
		public static readonly GetRelevantSectionsResponse EmptyResponse = new GetRelevantSectionsResponse
		{
			RelevantSections = new List<RelevantSection>(0)
		};

		[DataMember]
		public List<RelevantSection> RelevantSections { get; set; } = new List<RelevantSection>(0);
	}
}
