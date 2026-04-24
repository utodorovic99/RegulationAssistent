using AuditService.Model;

namespace AuditService.AuditTable
{
	internal interface IQueryTraceFormatter
	{
		string Format(QueryAudit queryAudit);
	}
}