namespace APIGatewayService.Context.Common
{
	/// <summary>
	/// Represents a processor that can handle requests of a specific type and produce results of a specific type. This interface defines a contract for processing logic, where implementations will take an object of type <typeparamref name="TProcessObject"/> and return a result of type <typeparamref name="TProcessingResult"/>. The processing is asynchronous, allowing for operations that may involve I/O or other time-consuming tasks.
	/// </summary>
	/// <typeparam name="TProcessObject">Type of object to process.</typeparam>
	/// <typeparam name="TProcessingResult">Type of processing result.</typeparam>
	internal interface IRequestProcessor<TProcessObject, TProcessingResult>
		where TProcessObject : IProcessingObject
		where TProcessingResult : IProcessingResult
	{
		/// <summary>
		/// Checks if the processor should handle the given object. This method allows for conditional processing based on the characteristics of the input object, enabling scenarios where multiple processors may exist and only one should handle a specific request. The implementation of this method will typically involve checking properties of the <paramref name="processObject"/> to determine if it meets certain criteria for processing.
		/// </summary>
		/// <param name="processObject">Object to process.</param>
		/// <returns><c>True</c> if processor should be executed; otherwise returns <c>false</c>.</returns>
		bool ShouldProcess(TProcessObject processObject);

		/// <summary>
		/// Processes the given object and returns a result. The implementation of this method will contain the core logic for handling the request, including any necessary validation, business logic, and response generation. The asynchronous nature of this method allows for efficient handling of requests without blocking the calling thread, making it suitable for high-throughput scenarios such as API gateways.
		/// </summary>
		/// <param name="processObject">Object to be processed.</param>
		/// <returns>Processing result.</returns>
		Task<TProcessingResult> ProcessRequestAsync(TProcessObject processObject);
	}
}