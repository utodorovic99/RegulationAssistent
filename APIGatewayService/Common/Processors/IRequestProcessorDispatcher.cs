using APIGatewayService.Context.Common;

namespace APIGatewayService.Common.Processors
{
	/// <summary>
	/// Abstract a dispatcher that manages the registration and invocation of request processors based on specific triggering conditions. This class allows for dynamic routing of processing requests to the appropriate processor implementations by evaluating the properties of incoming process objects against registered conditions. The dispatcher maintains a collection of processor descriptors, each containing a triggering condition and its associated processor, and uses this collection to determine which processor to execute for a given process object. This design enables flexible and extensible processing logic within the API Gateway service, allowing for easy addition of new processors without modifying existing code.
	/// </summary>
	/// <typeparam name="TProcessObject">Processed type of object.</typeparam>
	/// <typeparam name="TProcessingResult">Processed result.</typeparam>
	internal interface IRequestProcessorDispatcher<TProcessObject, TProcessingResult>
		where TProcessObject : IProcessingObject
		where TProcessingResult : IProcessingResult
	{
		/// <summary>
		/// Registers a new request processor to the dispatcher. The processor will be added to the internal list of processors and will be considered for execution when processing requests. This method allows for dynamic addition of processors at runtime, enabling flexible handling of various processing scenarios based on the properties of incoming process objects.
		/// </summary>
		/// <param name="processor">Processor to register.</param>
		void RegisterProcessor(IRequestProcessor<TProcessObject, TProcessingResult> processor);

		/// <summary>
		/// Dispatches the given request to the appropriate processor based on the registered triggering conditions. The dispatcher evaluates the properties of the incoming process object against the conditions defined in the registered processors and invokes the processor that matches the criteria. If a matching processor is found, it processes the request and returns the result; otherwise, it may return a default value or indicate that no suitable processor was found. This method is central to the dynamic routing of processing requests within the API Gateway service, allowing for efficient and flexible handling of various processing scenarios.
		/// </summary>
		/// <param name="request">Request to dispatch.</param>
		/// <param name="result">Processing result if dispatch is successful; otherwise default value.</param>
		void Dispatch(TProcessObject request, out TProcessingResult result);
	}
}