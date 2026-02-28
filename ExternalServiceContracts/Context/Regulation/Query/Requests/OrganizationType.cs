namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Defines known organization categories used when evaluating regulation queries.
	/// </summary>
	public enum OrganizationType : short
	{
		Company = 0,
		Government = 1,
		NonProfit = 2,
		Educational = 3,
		Healthcare = 4,
		Other = 5,
	}
}