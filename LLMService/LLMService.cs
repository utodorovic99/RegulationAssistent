using System.Fabric;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Context.Regulation.Embeddings.Requests;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ResponseService;

namespace LLMService
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class LLMService : StatefulService, ILLMService
	{
		private readonly IRpServiceProxyPool serviceProxyPool;
		private readonly ILLMAgent llmAgent;
		private readonly AsyncEmbeddingCreator embeddingCreator;
		private readonly AsyncEmbeddingPublisher embeddingPublisher;

		public LLMService(StatefulServiceContext context)
			: base(context)
		{
			serviceProxyPool = new RpServiceProxyPool();
			serviceProxyPool.RegisterFabricRP2Proxy<IAuditService>("fabric:/RegulationAssistent/AuditService", ServiceType.Stateful);
			serviceProxyPool.RegisterFabricRP2Proxy<IEmbeddingAsyncHandler>("fabric:/RegulationAssistent/DocumentIndexingService", ServiceType.Stateful);

			llmAgent = new LLMAgent(context.CodePackageActivationContext);
			embeddingPublisher = new AsyncEmbeddingPublisher(serviceProxyPool);
			embeddingCreator = new AsyncEmbeddingCreator(base.StateManager, embeddingPublisher, llmAgent);
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
			return new ServiceReplicaListener[1]
			{
				new ServiceReplicaListener(ctx =>
					new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}

		public async Task<CreateEmbeddingResponse> CreateEmbedding(CreateEmbeddingRequest request)
		{
			string inProgressOperation = string.Empty;
			string serviceName = nameof(LLMService);
			long requestId = request.RequestId;

			var auditServiceProxy = serviceProxyPool.GetProxy<IAuditService>();
			await auditServiceProxy.LogServiceEnter(requestId, serviceName);

			try
			{
				inProgressOperation = "LLM single embedding creation";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				var embedding = await llmAgent.CreateEmbeddingAsync(request.Text).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				return new CreateEmbeddingResponse { RequestId = request.RequestId, Embedding = embedding };
			}
			catch (Exception e)
			{
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, $"{inProgressOperation} failed with: {e}", "Failed");
				return new CreateEmbeddingResponse { RequestId = requestId, Embedding = Array.Empty<float>(), };
			}
			finally
			{
				await auditServiceProxy.LogServiceExit(requestId, serviceName);
			}
		}

		public async Task<bool> SubmitEmbeddingCreationBulkRequest(AsyncEmbeddingCreationRequest request)
		{
			string inProgressOperation = string.Empty;
			string serviceName = nameof(LLMService);
			long requestId = request.RequestId;

			var auditServiceProxy = serviceProxyPool.GetProxy<IAuditService>();
			await auditServiceProxy.LogServiceEnter(requestId, serviceName);

			try
			{
				inProgressOperation = "LLM bulk embedding creation";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				await embeddingCreator.ProcessAsync(request).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				return true;
			}
			catch (Exception e)
			{
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, $"{inProgressOperation} failed with: {e}", "Failed");
				return false;
			}
			finally
			{
				await auditServiceProxy.LogServiceExit(requestId, serviceName);
			}
		}

		public async Task<RegulationResponse> SubmitRegulationQuestion(RegulationLLMQuestion request)
		{
			string inProgressOperation = string.Empty;
			string serviceName = nameof(LLMService);
			long requestId = request.RequestId;

			var auditServiceProxy = serviceProxyPool.GetProxy<IAuditService>();
			await auditServiceProxy.LogServiceEnter(requestId, serviceName);

			try
			{
				inProgressOperation = "LLM response creation";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				RegulationResponse generatedResponse = await llmAgent.GenerateResponseAsync(request).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				return generatedResponse;
			}
			catch (Exception e)
			{
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, $"{inProgressOperation} failed with: {e}", "Failed");
				return RegulationResponse.CreateFailedResponse(requestId);
			}
			finally
			{
				await auditServiceProxy.LogServiceExit(requestId, serviceName);
			}
		}
	}
}
