namespace APIGatewayService.Context.Common.Request
{
	/// <summary>
	/// Interface representing a deserialized request object. This is a marker interface used to indicate that a class can be treated as a deserialized request for validation and processing purposes. Implementing this interface allows request validators to accept a common type while still working with specific request implementations.
	/// </summary>
	internal interface IDeserializedResponse
	{
	}
}
