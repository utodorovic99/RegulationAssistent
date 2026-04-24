using System.Runtime.Serialization;
using ExternalServiceContracts.Requests;

namespace AuditService.Model
{
	[DataContract]
	public sealed class QueryResponseAudit
	{
		[DataMember]
		public string Answer { get; set; } = string.Empty;

		[DataMember]
		public DateTime GeneratedTime { get; set; }

		[DataMember]
		public ResponseStatus Status { get; set; } = ResponseStatus.InsufficientData;

		[DataMember]
		public float Confidence { get; set; }
	}
}
