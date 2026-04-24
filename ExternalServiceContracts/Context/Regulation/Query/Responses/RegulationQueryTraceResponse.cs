using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Responses
{
	/// <summary>
	/// Represents the response to a regulation query trace request.
	/// </summary>
	public sealed class RegulationQueryTraceResponse : IJsonSerializableResponse
	{
		[JsonPropertyName("trace")]
		public string Trace { get; set; } = string.Empty;
	}
}