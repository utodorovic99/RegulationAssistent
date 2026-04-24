using System.Runtime.Serialization;

using System.Runtime.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Requests
{
	[DataContract]
	public sealed class AudibleRegulationLLMQuestion : AudibleMessage, IJsonSerializableRequest
	{
		[DataMember]
		public RegulationLLMQuestion Question { get; set; } = default!;
	}
}
