namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Represents the result of processing a request.
	/// </summary>
	internal interface IProcessingResult
	{
		/// <summary>
		/// Gets indicator whether processing is successfully executed.
		/// </summary>
		bool IsSuccessful { get; }
	}
}