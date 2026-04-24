using System.Threading.Tasks;
using ExternalServiceContracts.Requests;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	public interface IAuditService : IService
	{
		Task<long> LogRegulationQueryRequestReceived(string question, RegulationQueryContext additionalContext);

		Task LogServiceEnter(long requestId, string serviceFullName);

		Task LogServiceEvent(long requestId, string serviceFullName, string message, string status);

		Task LogServiceExit(long requestId, string serviceFullName);

		Task LogRegulationQueryResponseReceived(long requestId, string serviceFullName, string answer, ResponseStatus status, float confidence);

		Task<string> TraceLastRequest();
	}
}