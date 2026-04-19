using DocumentIndexingService.IndexingData;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace DocumentIndexingService.Parsers
{
	internal interface IDocumentParser
	{
		List<DocumentSectionIndex> ParseSections(byte[] fileBytes, DocumentItemDescriptor descriptor);
	}
}