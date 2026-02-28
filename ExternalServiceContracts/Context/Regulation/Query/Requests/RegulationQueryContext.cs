using System;
using System.Text;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Contextual information accompanying a regulation query (e.g., user, source, metadata).
	/// </summary>
	public sealed class RegulationQueryContext
	{
		/// <summary>
		/// Gets date when the query is made or applies.
		/// </summary>
		public DateOnly Date { get; init; } = default;

		/// <summary>
		/// Gets type of organization making the query.
		/// </summary>
		public OrganizationType OrganizationType { get; init; } = OrganizationType.Other;

		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			AppendSelfAsString(sb);

			return sb.ToString();
		}

		/// <inheritdoc/>
		public void AppendSelfAsString(StringBuilder sb)
		{
			sb.AppendLine($"Date: {Date}");
			sb.AppendLine($"Organization Type: {OrganizationType}");
		}
	}
}