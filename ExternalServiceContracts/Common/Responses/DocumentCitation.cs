using System.Text;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Common
{
	/// <summary>
	/// Represents a citation for a specific section of a document, including the document name, version, section
	/// identifier, and citation text.
	/// </summary>
	public sealed class DocumentCitation : IOptimizedStringOperations
	{
		/// <summary>
		/// Gets the name of the document.
		/// </summary>
		[JsonPropertyName("document-name")]
		public string DocumentName { get; init; } = string.Empty;

		/// <summary>
		/// Gets version of the document. This can be used to specify the edition or release of the document being cited.
		/// </summary>
		[JsonPropertyName("document-version")]
		public string Version { get; init; } = string.Empty;

		/// <summary>
		/// Gets section identifier within the document. This can be a specific section number, clause, or any other identifier that helps locate the relevant part of the document being cited.
		/// </summary>
		[JsonPropertyName("document-section")]
		public string SectionId { get; init; } = string.Empty;

		/// <summary>
		/// Gets the citation text that provides the specific reference or quote from the document. This can include the relevant passage, paragraph, or any other text that supports the citation.
		/// </summary>
		[JsonPropertyName("citation-content")]
		public string Citation { get; init; } = string.Empty;

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			AppendSelfAsString(sb);

			return sb.ToString();
		}

		/// <inheritdoc/>
		public void AppendSelfAsString(StringBuilder sb)
		{
			sb.AppendLine($"DocumentName: {DocumentName}");
			sb.AppendLine($"Version: {Version}");
			sb.AppendLine($"Section Id: {SectionId}");
			sb.AppendLine($"Citation: {Citation}");
		}
	}
}