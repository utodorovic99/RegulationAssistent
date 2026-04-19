using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Responses
{
	/// <summary>
	/// Represents the response to a regulation query, including all relevant information.
	/// </summary>
	public sealed class RegulationResponse : IJsonSerializableResponse
	{
		/// <summary>
		/// Represents a default response for failed regulation queries.
		/// This response indicates that the system was unable to provide an answer due to an internal failure.
		/// </summary>
		public static readonly RegulationResponse FailedResponse = new RegulationResponse
		{
			Answer = "System is unable to provide response due to internal failure.",
		};

		/// <summary>
		/// Gets a concise answer to the regulation query. This should provide a brief and direct response to the question asked.
		/// </summary>
		[JsonPropertyName("answer")]
		public string Answer { get; set; } = string.Empty;
	}
}