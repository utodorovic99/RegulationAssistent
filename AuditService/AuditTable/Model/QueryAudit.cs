using System.Runtime.Serialization;
using ExternalServiceContracts.Requests;

namespace AuditService.Model
{

	[DataContract]
	public sealed class QueryAudit
	{
		public QueryAudit()
		{
			ServiceTracing = new List<ServiceTraceContext>();
			Events = new List<ServiceEventTraceContext>();
		}

		[DataMember]
		public long QueryId { get; set; }

		[DataMember]
		public string QueryText { get; set; } = string.Empty;

		[DataMember]
		public DateTime ReceivedTime { get; set; }

		[DataMember]
		public RegulationQueryContext? AdditionalContext { get; set; }

		[DataMember]
		public QueryResponseAudit? ResponseAudit { get; set; }

		[DataMember]
		public List<ServiceTraceContext> ServiceTracing { get; set; } = new List<ServiceTraceContext>(0);

		[DataMember]
		public List<ServiceEventTraceContext> Events { get; set; }
	}
}
