using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Validator for <see cref="GetDocumentRequest"/>.
	/// </summary>
	internal class GetDocumentRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(IJsonSerializableRequest request, out string errorMessage)
		{
			errorMessage = string.Empty;

			if (request is not GetDocumentRequest getDocumentRequest)
			{
				errorMessage = "Invalid request type.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(getDocumentRequest.Title))
			{
				errorMessage = "Document title is required.";
				return false;
			}

			if (getDocumentRequest.VersionNumber <= 0)
			{
				errorMessage = "Version number must be greater than zero.";
				return false;
			}

			return true;
		}
	}
}