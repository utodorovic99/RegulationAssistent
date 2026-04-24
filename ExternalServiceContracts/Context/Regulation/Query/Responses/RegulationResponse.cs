using System.Collections.Generic;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;

namespace ExternalServiceContracts.Responses
{
	/// <summary>
	/// Represents the response to a regulation query, including all relevant information.
	/// </summary>
	public sealed class RegulationResponse : AudibleMessage, IJsonSerializableResponse
	{
		/// <summary>
		/// Represents a default response for failed regulation queries.
		/// This response indicates that the system was unable to provide an answer due to an internal failure.
		/// </summary>
		public static RegulationResponse CreateFailedResponse(long requestId) => new RegulationResponse
		{
			RequestId = requestId,
			Answer = "System is unable to provide response due to internal failure.",
			ShortAnswer = "System is unable to provide response due to internal failure.",
			Explanation = string.Empty,
			Citations = new List<DocumentCitation>(),
			Confidence = 0,
		};

		[JsonPropertyName("answer")]
		public string Answer { get; set; } = string.Empty;

		[JsonPropertyName("shortAnswer")]
		public string ShortAnswer { get; set; } = string.Empty;

		[JsonPropertyName("explanation")]
		public string Explanation { get; set; } = string.Empty;

		[JsonPropertyName("citations")]
		public List<DocumentCitation> Citations { get; set; } = new List<DocumentCitation>();

		[JsonPropertyName("confidence")]
		public float Confidence { get; set; }
	}
}