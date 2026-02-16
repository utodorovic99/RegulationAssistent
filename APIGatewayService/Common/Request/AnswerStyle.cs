namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Preferred answer presentation styles for regulation query responses.
	/// Serialized/deserialized as JSON strings when converters are configured.
	/// </summary>
	public enum AnswerStyle
	{
		ConciseWithCitations,
		Detailed,
		BulletPoints,
		Other
	}
}