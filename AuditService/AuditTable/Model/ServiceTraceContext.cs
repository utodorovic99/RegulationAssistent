using System.Runtime.Serialization;

namespace AuditService.Model
{
	[DataContract]
	public sealed class ServiceTraceContext
	{
		[DataMember]
		public string ServiceName { get; set; } = string.Empty;

		[DataMember]
		public DateTime EnterTime { get; set; }

		[DataMember]
		public DateTime ExitTime { get; set; }
	}
}
