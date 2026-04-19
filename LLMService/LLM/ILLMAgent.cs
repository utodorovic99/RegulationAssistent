using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;

namespace ResponseService
{
	internal interface ILLMAgent
	{
		Task<float[]> CreateEmbeddingAsync(string text);
		Task<float[][]> CreateEmbeddingsAsync(string[] texts);
		Task<string> GenerateResponseAsync(RegulationLLMQuestion request);
	}
}