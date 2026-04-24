using AuditService.Model;

namespace AuditService
{
	public interface IQueryAuditTable
	{
		Task StartQueryTrace(QueryAudit audit);

		Task StartServiceTraceAsync(long requestId, ServiceTraceContext serviceTrace);

		Task LogServiceEventAsync(long requestId, ServiceEventTraceContext serviceEvent);

		Task CompleteServiceTraceAsync(long requestId, string serviceName, DateTime timestamp);

		Task EndQueryTrace(long requestId, string serviceName, QueryResponseAudit audit);

		Task<string> TraceLastRequest();
	}
}