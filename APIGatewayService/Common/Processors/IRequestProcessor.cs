namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Represents a processor that can handle requests of a specific type and produce results of a specific type.
	/// </summary>
	internal interface IRequestProcessor
	{
		/// <summary>
		/// Checks if the processor should handle the given object.
		/// </summary>
		/// <param name="processObject">Object to process.</param>
		/// <returns><c>True</c> if processor should be executed; otherwise returns <c>false</c>.</returns>
		bool ShouldProcess(IProcessingObject processObject);

		/// <summary>
		/// Processes the given object and returns a result.
		/// </summary>
		/// <param name="processObject">Object to be processed.</param>
		/// <returns>Processing result.</returns>
		Task<IProcessingResult> ProcessRequestAsync(IProcessingObject processObject);
	}
}