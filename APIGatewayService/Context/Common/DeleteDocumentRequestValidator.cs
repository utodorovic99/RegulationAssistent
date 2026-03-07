using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;

namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Validator for <see cref="DeleteDocumentRequest"/>.
	/// </summary>
	internal class DeleteDocumentRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(ISerializableRequest request, out string errorMessage)
		{
			errorMessage = string.Empty;

			if (request is not DeleteDocumentRequest deleteDocumentRequest)
			{
				errorMessage = "Invalid request type.";
				return false;
			}

			if (string.IsNullOrWhiteSpace(deleteDocumentRequest.Title))
			{
				errorMessage = "Document title is required.";
				return false;
			}

			if (deleteDocumentRequest.VersionNumber <= 0)
			{
				errorMessage = "Version number must be greater than zero.";
				return false;
			}

			return true;
		}
	}
}
