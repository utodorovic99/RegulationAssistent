using System.Runtime.Serialization;

namespace AuditService.Model
{
	[DataContract]
	public sealed class ServiceTraceContext
	{
		public ServiceTraceContext()
		{
			Events = new List<ServiceEventTraceContext>();
		}

		[DataMember]
		public string ServiceName { get; set; } = string.Empty;

		[DataMember]
		public DateTimeOffset EnterTime { get; set; }

		[DataMember]
		public DateTimeOffset ExitTime { get; set; }

		[DataMember]
		public List<ServiceEventTraceContext> Events { get; set; }
	}
}
