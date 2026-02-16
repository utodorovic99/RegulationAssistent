using System.Diagnostics.Tracing;
using System.Fabric;

namespace APIGatewayService
{
	/// <summary>
	/// EventSource implementation for structured logging and diagnostics used by the service.
	/// </summary>
	[EventSource(Name = "MyCompany-RegulationAssistent-APIGatewayService")]
	internal sealed class ServiceEventSource : EventSource
	{
		public static readonly ServiceEventSource Current = new ServiceEventSource();

		/// <summary>
		/// Event keyword definitions used to categorize events.
		/// </summary>
		public static class Keywords
		{
			public const EventKeywords Requests = (EventKeywords)0x1L;
			public const EventKeywords ServiceInitialization = (EventKeywords)0x2L;
		}

		private const int MessageEventId = 1;
		private const int ServiceMessageEventId = 2;
		private const int ServiceTypeRegisteredEventId = 3;
		private const int ServiceHostInitializationFailedEventId = 4;
		private const int ServiceRequestStartEventId = 5;
		private const int ServiceRequestStopEventId = 6;

		// Instance constructor is private to enforce singleton semantics
		private ServiceEventSource()
			: base()
		{
		}

		/// <summary>
		/// Writes a formatted informational message. This overload accepts a format and arguments.
		/// </summary>
		/// <param name="message">Format string.</param>
		/// <param name="args">Format arguments.</param>
		[NonEvent]
		public void Message(string message, params object[] args)
		{
			if (this.IsEnabled())
			{
				string finalMessage = string.Format(message, args);
				Message(finalMessage);
			}
		}

		/// <summary>
		/// Writes a simple informational message event.
		/// </summary>
		/// <param name="message">Message to write.</param>
		[Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
		public void Message(string message)
		{
			if (this.IsEnabled())
			{
				WriteEvent(MessageEventId, message);
			}
		}

		/// <summary>
		/// Writes a service-scoped message that includes service and application metadata.
		/// </summary>
		/// <param name="serviceContext">Stateless service context for metadata.</param>
		/// <param name="message">Format string.</param>
		/// <param name="args">Format arguments.</param>
		[NonEvent]
		public void ServiceMessage(StatelessServiceContext serviceContext, string message, params object[] args)
		{
			if (this.IsEnabled())
			{
				string finalMessage = string.Format(message, args);
				ServiceMessage(
					serviceContext.ServiceName.ToString(),
					serviceContext.ServiceTypeName,
					serviceContext.InstanceId,
					serviceContext.PartitionId,
					serviceContext.CodePackageActivationContext.ApplicationName,
					serviceContext.CodePackageActivationContext.ApplicationTypeName,
					serviceContext.NodeContext.NodeName,
					finalMessage);
			}
		}

		/// <summary>
		/// Writes a detailed service message event including metadata and message text.
		/// </summary>
		/// <param name="serviceName">Service name.</param>
		/// <param name="serviceTypeName">Service type name.</param>
		/// <param name="replicaOrInstanceId">Instance id.</param>
		/// <param name="partitionId">Partition id.</param>
		/// <param name="applicationName">Application name.</param>
		/// <param name="applicationTypeName">Application type name.</param>
		/// <param name="nodeName">Node name.</param>
		/// <param name="message">Message text.</param>
		[Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
		private
#if UNSAFE
        unsafe
#endif
			void ServiceMessage(
			string serviceName,
			string serviceTypeName,
			long replicaOrInstanceId,
			Guid partitionId,
			string applicationName,
			string applicationTypeName,
			string nodeName,
			string message)
		{
#if !UNSAFE
			WriteEvent(ServiceMessageEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, message);
#else
            const int numArgs = 8;
            fixed (char* pServiceName = serviceName, pServiceTypeName = serviceTypeName, pApplicationName = applicationName, pApplicationTypeName = applicationTypeName, pNodeName = nodeName, pMessage = message)
            {
                EventData* eventData = stackalloc EventData[numArgs];
                eventData[0] = new EventData { DataPointer = (IntPtr) pServiceName, Size = SizeInBytes(serviceName) };
                eventData[1] = new EventData { DataPointer = (IntPtr) pServiceTypeName, Size = SizeInBytes(serviceTypeName) };
                eventData[2] = new EventData { DataPointer = (IntPtr) (&replicaOrInstanceId), Size = sizeof(long) };
                eventData[3] = new EventData { DataPointer = (IntPtr) (&partitionId), Size = sizeof(Guid) };
                eventData[4] = new EventData { DataPointer = (IntPtr) pApplicationName, Size = SizeInBytes(applicationName) };
                eventData[5] = new EventData { DataPointer = (IntPtr) pApplicationTypeName, Size = SizeInBytes(applicationTypeName) };
                eventData[6] = new EventData { DataPointer = (IntPtr) pNodeName, Size = SizeInBytes(nodeName) };
                eventData[7] = new EventData { DataPointer = (IntPtr) pMessage, Size = SizeInBytes(message) };

                WriteEventCore(ServiceMessageEventId, numArgs, eventData);
            }
#endif
		}

		/// <summary>
		/// Emits an event indicating that a service type was registered.
		/// </summary>
		/// <param name="hostProcessId">Host process id.</param>
		/// <param name="serviceType">Service type name.</param>
		[Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
		public void ServiceTypeRegistered(int hostProcessId, string serviceType)
		{
			WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
		}

		/// <summary>
		/// Emits an event when service host initialization fails.
		/// </summary>
		/// <param name="exception">Exception text.</param>
		[Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
		public void ServiceHostInitializationFailed(string exception)
		{
			WriteEvent(ServiceHostInitializationFailedEventId, exception);
		}

		/// <summary>
		/// Marks the start of a service request activity.
		/// </summary>
		/// <param name="requestTypeName">Request type name.</param>
		[Event(ServiceRequestStartEventId, Level = EventLevel.Informational, Message = "Service request '{0}' started", Keywords = Keywords.Requests)]
		public void ServiceRequestStart(string requestTypeName)
		{
			WriteEvent(ServiceRequestStartEventId, requestTypeName);
		}

		/// <summary>
		/// Marks the stop of a service request activity.
		/// </summary>
		/// <param name="requestTypeName">Request type name.</param>
		/// <param name="exception">Optional exception text if an error occurred.</param>
		[Event(ServiceRequestStopEventId, Level = EventLevel.Informational, Message = "Service request '{0}' finished", Keywords = Keywords.Requests)]
		public void ServiceRequestStop(string requestTypeName, string exception = "")
		{
			WriteEvent(ServiceRequestStopEventId, requestTypeName, exception);
		}

#if UNSAFE
        /// <summary>
        /// Helper to compute string size in bytes for unsafe WriteEventCore usage.
        /// </summary>
        private int SizeInBytes(string s)
        {
            if (s == null)
            {
                return 0;
            }
            else
            {
                return (s.Length + 1) * sizeof(char);
            }
        }
#endif
	}
}