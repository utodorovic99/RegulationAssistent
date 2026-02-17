using ExternalServiceContracts.Common;
using System.Linq;
using System.Text;

namespace RegulationAssistantChatClient.Extensions
{
	/// <summary>
	/// Extension class that provides a predefined failed response for regulation queries. This can be used to return a consistent response when the system encounters an internal failure while processing a regulation query.
	/// </summary>
	internal static class RegulationQueryResponseExtensions
	{
		/// <summary>
		/// Returns a string representation of the <see cref="RegulationQueryResponse"/> instance formatted for use as a chatbot response. The string includes the short answer, explanation, and citations in a readable format.
		/// </summary>
		/// <param name="caller"></param>
		/// <returns></returns>
		public static string AsChatbotResponseString(this RegulationQueryResponse caller)
		{
			StringBuilder sb = new StringBuilder();

			if (!string.IsNullOrEmpty(caller.ShortAnswer))
			{
				sb.AppendLine(caller.ShortAnswer);
			}

			if (!string.IsNullOrEmpty(caller.Explanation))
			{
				sb.AppendLine($"Explanation: {caller.Explanation}");
			}

			var citationsAsList = caller.Citations?.ToList();
			if (citationsAsList?.Count > 0)
			{
				sb.AppendLine($"Citations:");

				foreach (var citation in caller.Citations)
				{
					sb.AppendLine($"Document: {citation.DocumentName},");
					sb.AppendLine($"Version: {citation.Version},");
					sb.AppendLine($"Section: {citation.SectionId},");
					sb.AppendLine($"Quote: {citation.Citation}.");
					sb.AppendLine();
				}
			}

			sb.AppendLine($"Confidence: {caller.Confidence:P2}");

			return sb.ToString();
		}
	}
}