using System.Text;
using CommonSDK;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentIndexingService.IndexingData;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace DocumentIndexingService.Parsers
{
	internal sealed class WordDocumentParser : IDocumentParser
	{
		public List<DocumentSectionIndex> ParseSections(byte[] fileBytes, DocumentItemDescriptor descriptor)
		{
			var sections = new List<DocumentSectionIndex>();
			if (fileBytes == null || fileBytes.Length == 0) return sections;

			// Only attempt to open OpenXML Word documents (.docx). The WordprocessingDocument
			// API works with ZIP-based OOXML packages; trying to open a legacy binary .doc
			// will throw a "file is corrupted" exception. Detect the ZIP header and skip
			// parsing when the file is not a DOCX.
			if (!IsDocx(fileBytes)) return sections;

			using (var ms = new MemoryStream(fileBytes))
			using (var word = WordprocessingDocument.Open(ms, false))
			{
				var body = word.MainDocumentPart?.Document?.Body;
				if (body == null) return sections;

				sections.AddRange(ExtractSectionsFromBody(body, descriptor));
			}

			return sections;
		}

		private static bool IsDocx(byte[] bytes)
		{
			// DOCX files are ZIP packages and start with PK\x03\x04 (0x50 0x4B 0x03 0x04)
			return bytes != null && bytes.Length >= 4
				&& bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04;
		}

		private static List<DocumentSectionIndex> ExtractSectionsFromBody(Body body, DocumentItemDescriptor descriptor)
		{
			var sections = new List<DocumentSectionIndex>();
			var currentArticleText = new StringBuilder();
			uint currentChapter = 0;
			uint currentArticle = 0;
			int paragraphIndex = 0;

			foreach (var para in GetParagraphs(body))
			{
				string text = para.InnerText?.Replace('\r', ' ').Replace('\n', ' ').Trim() ?? string.Empty;
				paragraphIndex++;

				if (IsArticleHeader(para, text))
				{
					// When a new article starts, save the previous one if it has content.
					if (currentArticleText.Length > 0)
					{
						string sectionId = BuildSectionId(descriptor, currentChapter, currentArticle, 0); // Paragraph number is not needed for article-based sections
						sections.Add(CreateSection(sectionId, descriptor, currentChapter, currentArticle, currentArticleText.ToString().Trim()));
						currentArticleText.Clear();
					}

					// Extract chapter and article numbers from the new header.
					var (chapter, article) = ExtractChapterArticle(text);
					currentChapter = chapter;
					currentArticle = article;
					continue; // Skip adding the header text to the content.
				}

				if (IsParagraphHeading(para, text))
				{
					continue; // Skip other headings.
				}

				if (!string.IsNullOrEmpty(text))
				{
					currentArticleText.AppendLine(text);
				}
			}

			// Add the last processed article if it has content.
			if (currentArticleText.Length > 0)
			{
				string sectionId = BuildSectionId(descriptor, currentChapter, currentArticle, 0);
				sections.Add(CreateSection(sectionId, descriptor, currentChapter, currentArticle, currentArticleText.ToString().Trim()));
			}

			return sections;
		}

		private static bool IsArticleHeader(Paragraph para, string text)
		{
			var style = para.ParagraphProperties?
				.ParagraphStyleId?.Val?.Value?
				.ToLowerInvariant();

			if (!string.IsNullOrEmpty(style))
			{
				if (style.Contains("clan"))
					return true;
			}

			return false;
		}

		private static (uint chapter, uint article) ExtractChapterArticle(string text)
		{
			// This is a placeholder for logic to extract chapter and article numbers.
			// For now, it will just extract the article number if it finds one.
			uint chapter = 0; // Chapter extraction logic needs to be defined based on document structure.
			uint article = 0;

			var match = System.Text.RegularExpressions.Regex.Match(text.ToLowerInvariant(), @"član\s*(\d+)");
			if (match.Success && uint.TryParse(match.Groups[1].Value, out uint art))
			{
				article = art;
			}

			return (chapter, article);
		}

		private static IEnumerable<Paragraph> GetParagraphs(Body body)
		{
			return body.Elements<Paragraph>();
		}

		// New ID format: titleName::version:chapter:article:paragraph
		private static string BuildSectionId(DocumentItemDescriptor descriptor, uint chapter, uint article, int paragraphNumber)
		{
			string titleName = descriptor != null && !string.IsNullOrEmpty(descriptor.Title)
				? NamingHelper.SanitizeName(descriptor.Title)
				: NamingHelper.SanitizeName(descriptor?.ToString() ?? string.Empty);

			int version = descriptor != null ? descriptor.VersionNumber : 0;

			return $"{titleName}::{version}:{chapter}:{article}:{paragraphNumber}";
		}

		private static DocumentSectionIndex CreateSection(string id, DocumentItemDescriptor descriptor, uint chapter, uint article, string text)
		{
			return new DocumentSectionIndex
			{
				Id = id,
				Vector = Array.Empty<float>(),

				Payload = new DocumentSectionData
				{
					DocumentId = descriptor.Id,
					Law = descriptor.Title,
					Chapter = chapter,
					Article = article,
					ValidFrom = descriptor.ValidFrom,
					ValidTo = descriptor.ValidTo,
					Text = text
				}
			};
		}

		private static bool IsParagraphHeading(Paragraph para, string text)
		{
			// Consider explicit heading styles as headings
			var pPr = para.ParagraphProperties;
			if (pPr?.ParagraphStyleId != null)
			{
				var style = pPr.ParagraphStyleId.Val?.Value?.ToLowerInvariant() ?? string.Empty;
				if (style.Contains("heading")) return true;
			}

			// Exclude short lines that might be subheadings but are not article headers.
			int wordCount = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
			if (text.Length < 100 && wordCount <= 10)
			{
				if (text.EndsWith(":") || text.EndsWith("-"))
				{
					return true;
				}
			}

			return false;
		}
	}
}
