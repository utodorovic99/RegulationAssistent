using System.Runtime.Serialization;
using System.Runtime.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	[DataContract]
	public abstract class AudibleMessage
	{
		[DataMember]
		public long RequestId { get; set; }
	}
}
