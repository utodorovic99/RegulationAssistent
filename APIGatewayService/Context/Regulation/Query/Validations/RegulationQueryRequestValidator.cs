using APIGatewayService.Context.Common;
using CommonSDK;
using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;

namespace APIGatewayService.Context.RegulationQuery
{
	/// <summary>
	/// Provides methods to validate a <see cref="RegulationQueryRequest"/>.
	/// </summary>
	internal sealed class RegulationQueryRequestValidator : IRequestValidator
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(ISerializableRequest? req, out string? error)
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

			if (!TryValidatePreferences(reqCasted.Preferences, out error))
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

			if (!Enum.IsDefined(typeof(OrganizationType), ctx.OrganizationType))
			{
				error = "invalid_organization_type";
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates the query preferences (language, answer style).
		/// </summary>
		/// <param name="pref">Preferences to validate.</param>
		/// <param name="error">Error code when validation fails.</param>
		private bool TryValidatePreferences(QueryPreferences? pref, out string? error)
		{
			error = null;

			if (pref == null)
			{
				error = "missing_preferences";
				return false;
			}

			if (!Enum.IsDefined(typeof(Language), pref.Language))
			{
				error = "invalid_language";
				return false;
			}

			if (!Enum.IsDefined(typeof(AnswerStyle), pref.AnswerStyle))
			{
				error = "invalid_preferences";
				return false;
			}

			return true;
		}
	}
}