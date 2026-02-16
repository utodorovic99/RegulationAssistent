using APIGatewayService.Context.Common;
using APIGatewayService.Context.Common.Request;

namespace APIGatewayService.Context.RegulationQuery
{
	/// <summary>
	/// Provides methods to validate a <see cref="RegulationQueryRequest"/> and its associated components, including
	/// context and preferences, ensuring that all required fields are present and valid.
	/// </summary>
	/// <remarks>
	/// This class contains static methods for validating different parts of a regulation query request. It
	/// ensures that the request, context, and preferences conform to the expected structure and values. Validation errors
	/// are returned as error codes to help identify specific issues.</remarks>
	internal class RegulationQueryRequestValidator : IRequestValidator<RegulationQueryRequest>
	{
		/// <inheritdoc/>
		public bool TryValidateRequest(IDeserializedRequest? req, out string? error)
		{
			RegulationQueryRequest? reqCasted = req as RegulationQueryRequest;

			if (req is null)
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

			error = null;
			return true;
		}

		/// <summary>
		/// Validates the query context (date, organization type, jurisdiction).
		/// Treats default(DateOnly) as missing.
		/// </summary>
		/// <param name="ctx">Context to validate.</param>
		/// <param name="error">Error code when validation fails.</param>
		private bool TryValidateContext(RegulationQueryContext? ctx, out string? error)
		{
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

			if (string.IsNullOrWhiteSpace(ctx.Jurisdiction))
			{
				error = "invalid_context";
				return false;
			}

			if (!Enum.IsDefined(typeof(OrganizationType), ctx.OrganizationType))
			{
				error = "invalid_organization_type";
				return false;
			}

			error = null;
			return true;
		}

		/// <summary>
		/// Validates the query preferences (language, answer style).
		/// </summary>
		/// <param name="pref">Preferences to validate.</param>
		/// <param name="error">Error code when validation fails.</param>
		private bool TryValidatePreferences(QueryPreferences? pref, out string? error)
		{
			if (pref == null)
			{
				error = "missing_preferences";
				return false;
			}

			// Ensure language is a defined enum value
			if (!Enum.IsDefined(typeof(SupportedLanguage), pref.Language))
			{
				error = "invalid_language";
				return false;
			}

			if (!Enum.IsDefined(typeof(AnswerStyle), pref.AnswerStyle))
			{
				error = "invalid_preferences";
				return false;
			}

			error = null;
			return true;
		}
	}
}