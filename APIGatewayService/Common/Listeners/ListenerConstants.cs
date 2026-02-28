namespace APIGatewayService.Common.Listeners
{
	/// <summary>
	/// Shared constants used by communication listeners infrastructure.
	/// </summary>
	/// <remarks>
	/// String constants are used to avoid unnecessary pressure on GC high-activity scenarios.
	/// </remarks>
	internal static class ListenerConstants
	{
		/// <summary>
		/// Shared response type for UTF - 8 encoded JSON responses.
		/// </summary>
		public const string ResponseTypeUTF8Json = "application/json; charset=utf-8";
	}
}