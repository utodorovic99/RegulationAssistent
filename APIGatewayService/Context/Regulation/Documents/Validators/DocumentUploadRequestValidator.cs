using System.Fabric;
using APIGatewayService.Context.Common;
using CommonSDK;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Requests;

namespace APIGatewayService.Context.Regulation.Documents
{
	/// <summary>
	/// Validator for <see cref="DocumentUploadRequest"/>.
	/// </summary>
	internal sealed class DocumentUploadRequestValidator : IRequestValidator
	{
		private readonly HashSet<DocumentFormat> supportedDocumentFormats = new HashSet<DocumentFormat>(1)
		{
			DocumentFormat.Docx,
		};

		private readonly StatelessServiceContext serviceContext;
		private readonly IRpServiceProxyPool serviceProxyPool;

		public DocumentUploadRequestValidator(StatelessServiceContext serviceContext, IRpServiceProxyPool serviceProxyPool)
		{
			this.serviceContext = serviceContext;
			this.serviceProxyPool = serviceProxyPool;
		}

		/// <inheritdoc/>
		public bool TryValidateRequest(IJsonSerializableRequest? req, out string error)
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

			if (!supportedDocumentFormats.Contains(casted.Format))
			{
				error = "invalid_format";
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

			if (!ValidateDateSpan(casted))
			{
				error = "older_than_last_version";
				return false;
			}

			return true;
		}

		private bool ValidateDateSpan(DocumentUploadRequest casted)
		{
			try
			{
				var storageProxy = serviceProxyPool.GetProxy<ExternalServiceContracts.Services.IDocumentStorageService>();
				var latest = storageProxy.GetLatestDocumentByTitle(casted.Title).GetAwaiter().GetResult();

				if (latest == null)
				{
					return true;
				}

				if (casted.ValidFrom < latest.ValidFrom)
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(serviceContext, $"DocumentUploadRequestValidator: Failed to validate against existing documents: {ex}");
				return false;
			}

			return true;
		}
	}
}