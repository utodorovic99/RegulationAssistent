using APIGatewayService.Context.Common;

namespace APIGatewayService.Common.Processors
{
	/// <summary>
	/// Abstract a dispatcher that manages the registration and invocation of request processors based on specific triggering conditions.
	/// </summary>
	internal interface IRequestProcessorDispatcher
	{
		/// <summary>
		/// Registers a new request processor to the dispatcher.
		/// </summary>
		/// <param name="processor">Processor to register.</param>
		void RegisterProcessor(IRequestProcessor processor);

		/// <summary>
		/// Dispatches the given request to the appropriate processor based on the registered triggering conditions.
		/// </summary>
		/// <param name="request">Request to dispatch.</param>
		/// <param name="result">Processing result if dispatch is successful; otherwise default value.</param>
		void Dispatch(IProcessingObject request, out IProcessingResult result);
	}
}