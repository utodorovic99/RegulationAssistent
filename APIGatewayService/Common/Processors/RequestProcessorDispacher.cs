using APIGatewayService.Context.Common;

namespace APIGatewayService.Common.Processors
{
	/// <summary>
	/// Represents a dispatcher that manages the registration and invocation of request processors based on specific triggering conditions. This class allows for dynamic routing of processing requests to the appropriate processor implementations by evaluating the properties of incoming process objects against registered conditions. The dispatcher maintains a collection of processor descriptors, each containing a triggering condition and its associated processor, and uses this collection to determine which processor to execute for a given process object. This design enables flexible and extensible processing logic within the API Gateway service, allowing for easy addition of new processors without modifying existing code.
	/// </summary>
	internal sealed class RequestProcessorDispatcher : IRequestProcessorDispatcher
	{
		private readonly IList<IRequestProcessor> processors;

		/// <summary>
		/// Initializes new instance of <see cref="RequestProcessorDispatcher"/>.
		/// </summary>
		public RequestProcessorDispatcher()
			: this(processors: new List<IRequestProcessor>(0))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestProcessorDispatcher"/> class with the specified list of processors.
		/// </summary>
		/// <param name="processors">Processors available for execution.</param>
		internal RequestProcessorDispatcher(IList<IRequestProcessor> processors)
		{
			this.processors = processors;
		}

		/// <inheritdoc/>
		public void RegisterProcessor(IRequestProcessor processor)
		{
			processors.Add(processor);
		}

		/// <inheritdoc/>
		public async Task<IProcessingResult> DispatchAsync(IProcessingObject request)
		{
			foreach (var processor in processors)
			{
				if (processor.ShouldProcess(request))
				{
					return await processor.ProcessRequestAsync(request);
				}
			}

			return StatusProcessResult.Failed;
		}
	}
}