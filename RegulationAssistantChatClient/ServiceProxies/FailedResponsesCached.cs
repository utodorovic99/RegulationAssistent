using ExternalServiceContracts.Common;
using System.Linq;

namespace RegulationAssistantChatClient
{
	/// <summary>
	/// Provides cached failed responses for all service calls.
	/// </summary>
	internal static class FailedResponsesCached
	{
		/// <summary>
		/// Represents a default response for failed regulation queries.
		/// This response indicates that the system was unable to provide an answer due to an internal failure.
		/// </summary>
		public static readonly RegulationQueryResponse FailedRegulationQueryResponse = new RegulationQueryResponse
		{
			ShortAnswer = "System is unable to provide response due to internal failure.",
			Explanation = string.Empty,
			Citations = Enumerable.Empty<DocumentCitation>(),
			Confidence = 1.0f
		};
	}
}