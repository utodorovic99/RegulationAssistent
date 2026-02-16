namespace APIGatewayService.Context.RegulationQuery
{
	/// <summary>
	/// Contextual information accompanying a regulation query (e.g., user, source, metadata).
	/// </summary>
	public sealed class RegulationQueryContext
	{
		/// <summary>
		/// The date when the query is made or applies.
		/// </summary>
		public DateOnly Date { get; init; }

		/// <summary>
		/// The type of organization making the query.
		/// </summary>
		public OrganizationType OrganizationType { get; init; } = OrganizationType.Other;

		/// <summary>
		/// The jurisdiction (country/region) the query pertains to.
		/// </summary>
		public string Jurisdiction { get; init; } = default!;
	}
}