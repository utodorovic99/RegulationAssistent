using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Validator for <see cref="DocumentUploadRequest"/>.
	/// </summary>
	internal sealed class DocumentUploadRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(ISerializableRequest? req, out string error)
		{
			error = null;

			if (req == null)
			{
				error = "missing_request";
				return false;
			}

			DocumentUploadRequest casted = req as DocumentUploadRequest;
			if (casted == null)
			{
				error = "invalid_request_type";
				return false;
			}

			if (string.IsNullOrWhiteSpace(casted.Title))
			{
				error = "missing_title";
				return false;
			}

			if (casted.FileBytes == null || casted.FileBytes.Length == 0)
			{
				error = "missing_file";
				return false;
			}

			return true;
		}
	}
}