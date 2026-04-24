namespace ExternalServiceContracts.Requests
{
	public enum ResponseStatus : short
	{
		Successful = 0,
		Partial = 1,
		InsufficientData = -1,
		Error = -2,
	}
}
