using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	/// <summary>
	/// Service contract for response generation (e.g., LLM-backed responses).
	/// </summary>
	public interface ILLMService : IService
	{
		Task<bool> SubmitEmbeddingCreationBulkRequest(AsyncEmbeddingCreationRequest request);
		Task<CreateEmbeddingResponse> CreateEmbedding(CreateEmbeddingRequest request);
		Task<RegulationResponse> SubmitRegulationQuestion(RegulationLLMQuestion request);
	}
}