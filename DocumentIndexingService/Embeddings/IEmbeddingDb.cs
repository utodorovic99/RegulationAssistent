using DocumentIndexingService.IndexingData;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;

namespace DocumentIndexingService.Embeddings
{
	internal interface IEmbeddingDb
	{
		Task StoreIndicesAsync(List<DocumentSectionIndex> entries);
		Task DeleteIndices(string rootId);
		Task<GetRelevantSectionsResponse> GetIndexedResults(GetRelevantSectionsRequest request);
	}
}
