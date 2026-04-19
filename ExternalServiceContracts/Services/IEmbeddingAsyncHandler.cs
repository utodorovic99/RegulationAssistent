using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	public interface IEmbeddingAsyncHandler : IService
	{
		Task<bool> ProcessEmbeddingChunkCreated(SubmitAsyncEmbeddingsRequest response);
	}
}
