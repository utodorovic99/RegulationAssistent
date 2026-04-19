using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Requests;

namespace APIGatewayService.Context.RegulationQuery
{
	/// <summary>
	/// Provides methods to validate a <see cref="RegulationQueryRequest"/>.
	/// </summary>
	internal sealed class RegulationQueryRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(IJsonSerializableRequest? req, out string? error)
		{
			error = null;

			RegulationQueryRequest? reqCasted = req as RegulationQueryRequest;

			if (req == null)
			{
				error = "missing_request";
				return false;
			}

			if (string.IsNullOrWhiteSpace(reqCasted.Question))
			{
				error = "missing_question";
				return false;
			}

			if (!TryValidateContext(reqCasted.Context, out error))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates the query context (date, organization type).
		/// </summary>
		/// <param name="ctx">Context to validate.</param>
		/// <param name="error">Error code when validation fails.</param>
		private bool TryValidateContext(RegulationQueryContext? ctx, out string? error)
		{
			error = null;

			if (ctx == null)
			{
				error = "missing_context";
				return false;
			}

			if (ctx.Date == default)
			{
				error = "missing_context_date";
				return false;
			}

			return true;
		}
	}
}