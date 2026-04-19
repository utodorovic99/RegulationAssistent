using System.Threading.Tasks;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	public interface IRegulationQuery : IService
	{
		Task<RegulationResponse> SubmitQuestion(RegulationQueryRequest request);
	}
}