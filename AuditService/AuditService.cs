using System.Fabric;
using AuditService.Model;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace AuditService
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class AuditService : StatefulService, IAuditService
	{
		private readonly IUniqueRequestIdGenerator requestIdGenerator;
		private readonly IQueryAuditTable queryAuditTable;

		public AuditService(StatefulServiceContext context)
			: base(context)
		{
			requestIdGenerator = new UniqueRequestIdGenerator(this.StateManager);
			queryAuditTable = new QueryAuditTable(this.StateManager);
		}

		/// <summary>
		/// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
		/// </summary>
		/// <remarks>
		/// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
		/// </remarks>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new[]
			{
				new ServiceReplicaListener(ctx => new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}

		public async Task<long> LogRegulationQueryRequestReceived(string question, RegulationQueryContext additionalContext)
		{
			long requestId = requestIdGenerator.GenerateUniqueRequestId();
			QueryAudit queryAudit = new QueryAudit
			{
				QueryId = requestId,
				QueryText = question,
				ReceivedTime = DateTime.Now,
				AdditionalContext = additionalContext,
			};

			await queryAuditTable.StartQueryTrace(queryAudit).ConfigureAwait(false);

			return requestId;
		}

		public async Task LogServiceEnter(long requestId, string serviceFullName)
		{
			var serviceTraceContext = new ServiceTraceContext
			{
				ServiceName = serviceFullName,
				EnterTime = DateTime.Now,
			};

			await queryAuditTable.StartServiceTraceAsync(requestId, serviceTraceContext);
		}

		public async Task LogServiceEvent(long requestId, string serviceFullName, string message, string status)
		{
			var servieEvent = new ServiceEventTraceContext()
			{
				Timestamp = DateTime.Now,
				Message = message,
				Status = status,
			};

			await queryAuditTable.LogServiceEventAsync(requestId, serviceFullName, servieEvent).ConfigureAwait(false);
		}

		public async Task LogServiceExit(long requestId, string serviceFullName)
		{
			await queryAuditTable.CompleteServiceTraceAsync(requestId, serviceFullName, DateTime.Now);
		}

		public async Task LogRegulationQueryResponseReceived(long requestId, string serviceFullName, string answer, ResponseStatus status, float confidence)
		{
			var responseAudit = new QueryResponseAudit
			{
				Answer = answer,
				GeneratedTime = DateTime.Now,
				Status = status,
				Confidence = confidence,
			};


			await queryAuditTable.EndQueryTrace(requestId, serviceFullName, responseAudit);
		}

		public async Task<string> TraceLastRequest()
		{
			return await queryAuditTable.TraceLastRequest();
		}
	}
}
