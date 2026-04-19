using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	/// <summary>
	/// Marker interface for document indexing read operations.
	/// Empty by design; implemented by the indexing service.
	/// </summary>
	public interface IDocumentIndexReader : IService
	{
		Task<GetRelevantSectionsResponse> GetIndexedResults(GetRelevantSectionsRequest request);
	}
}