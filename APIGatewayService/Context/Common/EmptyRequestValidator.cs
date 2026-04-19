using CommonSDK;

namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Validator for empty requests (always valid).
	/// </summary>
	internal sealed class EmptyRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(IJsonSerializableRequest? request, out string? error)
		{
			error = null;
			return true;
		}
	}
}