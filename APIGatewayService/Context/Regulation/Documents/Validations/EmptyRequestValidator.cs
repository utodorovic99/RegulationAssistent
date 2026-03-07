using APIGatewayService.Context.Common;
using CommonSDK;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Validator for empty requests (always valid).
	/// </summary>
	internal sealed class EmptyRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(ISerializableRequest? request, out string? error)
		{
			error = null;
			return true;
		}
	}
}
