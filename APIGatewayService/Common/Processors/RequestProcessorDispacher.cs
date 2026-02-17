using APIGatewayService.Context.Common;

namespace APIGatewayService.Common.Processors
{
	/// <summary>
	/// Represents a dispatcher that manages the registration and invocation of request processors based on specific triggering conditions. This class allows for dynamic routing of processing requests to the appropriate processor implementations by evaluating the properties of incoming process objects against registered conditions. The dispatcher maintains a collection of processor descriptors, each containing a triggering condition and its associated processor, and uses this collection to determine which processor to execute for a given process object. This design enables flexible and extensible processing logic within the API Gateway service, allowing for easy addition of new processors without modifying existing code.
	/// </summary>
	/// <typeparam name="TProcessObject">Processed type of object.</typeparam>
	/// <typeparam name="TProcessingResult">Processed result.</typeparam>
	internal sealed class RequestProcessorDispatcher<TProcessObject, TProcessingResult> : IRequestProcessorDispatcher<TProcessObject, TProcessingResult> where TProcessObject : IProcessingObject
		where TProcessingResult : IProcessingResult
	{
		private readonly IList<IRequestProcessor<TProcessObject, TProcessingResult>> processors;

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestProcessorDispatcher{TProcessObject, TProcessingResult}"/> class with an empty list of processors.
		/// </summary>
		public RequestProcessorDispatcher()
			: this(processors: new List<IRequestProcessor<TProcessObject, TProcessingResult>>(0))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestProcessorDispatcher{TProcessObject, TProcessingResult}"/> class with the specified list of processors.
		/// </summary>
		/// <param name="processors">Processors available for execution.</param>
		internal RequestProcessorDispatcher(IList<IRequestProcessor<TProcessObject, TProcessingResult>> processors)
		{
			this.processors = processors;
		}

		/// <inheritdoc/>
		public void RegisterProcessor(IRequestProcessor<TProcessObject, TProcessingResult> processor)
		{
			processors.Add(processor);
		}

		/// <inheritdoc/>
		public void Dispatch(TProcessObject request, out TProcessingResult result)
		{
			result = default;

			foreach (var processor in processors)
			{
				if (processor.ShouldProcess(request))
				{
					processor.ProcessRequestAsync(request).GetAwaiter().GetResult();
					return;
				}
			}
		}
	}
}