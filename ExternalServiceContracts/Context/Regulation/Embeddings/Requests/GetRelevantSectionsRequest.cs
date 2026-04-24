using System.Runtime.Serialization;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Requests
{
	[DataContract]
	public sealed class GetRelevantSectionsRequest : AudibleMessage
	{
		[DataMember]
		public float[] QuestionEmbedding { get; set; } = System.Array.Empty<float>();

		[DataMember]
		public RegulationQueryContext QuestionContext { get; set; } = default!;

		[DataMember]
		public int NumberOfResults { get; set; }

		[DataMember]
		public float ScoreThreshold { get; set; } = 0.7f;
	}
}
