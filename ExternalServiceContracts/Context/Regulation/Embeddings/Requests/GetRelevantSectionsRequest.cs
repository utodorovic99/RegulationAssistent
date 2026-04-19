namespace ExternalServiceContracts.Requests
{
	public class GetRelevantSectionsRequest
	{
		public float[] QuestionEmbedding { get; set; }
		public RegulationQueryContext QuestionContext { get; set; }
		public int NumberOfResults { get; set; }
	}
}
