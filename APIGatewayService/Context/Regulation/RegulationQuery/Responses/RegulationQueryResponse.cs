using APIGatewayService.Context.Common.Request;
using System.Text.Json.Serialization;

namespace APIGatewayService.Context.Regulation
{
	/// <summary>
	/// Represents the response to a regulation query, including a short answer, an explanation, relevant document citations, and a confidence score indicating the reliability of the answer.
	/// </summary>
	public sealed class RegulationQueryResponse : IDeserializedResponse
	{
		/// <summary>
		/// Gets a concise answer to the regulation query. This should provide a brief and direct response to the question asked, summarizing the key information derived from the relevant regulations.
		/// </summary>
		[JsonPropertyName("short-answer")]
		public string? ShortAnswer { get; init; } = string.Empty;

		/// <summary>
		/// Gets a detailed explanation that provides context and reasoning behind the short answer. This should elaborate on how the answer was derived, referencing specific regulations, interpretations, or any relevant information that supports the conclusion presented in the short answer.
		/// </summary>
		[JsonPropertyName("explanation")]
		public string? Explanation { get; init; } = string.Empty;

		/// <summary>
		/// Gets a collection of document citations that are relevant to the regulation query. Each citation should include information about the document name, version, section identifier, and the specific citation text that supports the answer provided. This allows users to reference the original sources of information for further reading or verification.
		/// </summary>
		[JsonPropertyName("citations")]
		public IEnumerable<DocumentCitation> Citations { get; init; } = Enumerable.Empty<DocumentCitation>();

		/// <summary>
		/// Gets a confidence score between 0.0 and 1.0 that indicates the reliability of the answer provided. A score closer to 1.0 suggests a higher level of confidence in the accuracy and relevance of the answer, while a score closer to 0.0 indicates lower confidence. This can help users assess the trustworthiness of the response and decide whether to seek additional information or verification.
		/// </summary>
		[JsonPropertyName("confidence")]
		public float Confidence { get; init; } = 0.0f;
	}
}
