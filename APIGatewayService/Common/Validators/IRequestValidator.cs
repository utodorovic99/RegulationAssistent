using CommonSDK;

namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Interface for validating request objects.
	/// </summary>
	internal interface IRequestValidator
	{
		/// <summary>
		/// Validates the request.
		/// Returns true when valid; otherwise returns false and sets <paramref name="error"/> to an error code.
		/// </summary>
		/// <param name="req">Deserialized request.</param>
		/// <param name="error">Error code when validation fails.</param>
		bool TryValidateRequest(IJsonSerializableRequest? req, out string error);
	}
}