using System.Runtime.Serialization;

namespace AuditService.Model
{
	[DataContract]
	public sealed class ServiceEventTraceContext
	{
		[DataMember]
		public DateTimeOffset Timestamp { get; set; }

		[DataMember]
		public string Message { get; set; } = string.Empty;

		[DataMember]
		public string Status { get; set; } = string.Empty;
	}
}
