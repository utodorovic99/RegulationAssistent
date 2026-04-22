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
			serviceProxyPool.RegisterFabricRP2Proxy<IEmbeddingAsyncHandler>("fabric:/RegulationAssistent/DocumentIndexingService", ServiceType.Stateful);

			llmAgent = new LLMAgent(context.CodePackageActivationContext);
			embeddingPublisher = new AsyncEmbeddingPublisher(serviceProxyPool);
			embeddingCreator = new AsyncEmbeddingCreator(base.StateManager, embeddingPublisher, llmAgent);
		}

		public async Task<CreateEmbeddingResponse> CreateEmbedding(CreateEmbeddingRequest request)
		{
			if (request == null || string.IsNullOrEmpty(request.Text))
			{
				return new CreateEmbeddingResponse { Embedding = System.Array.Empty<float>() };
			}

			try
			{
				var embedding = await llmAgent.CreateEmbeddingAsync(request.Text).ConfigureAwait(false);
				return new CreateEmbeddingResponse { Embedding = embedding };
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"CreateEmbedding failed: {ex.Message}");
				return new CreateEmbeddingResponse { Embedding = System.Array.Empty<float>() };
			}
		}

		public async Task<bool> SubmitEmbeddingCreationBulkRequest(AsyncEmbeddingCreationRequest request)
		{
			if (request?.Texts?.Length > 0)
			{
				try
				{
					await embeddingCreator.ProcessAsync(request).ConfigureAwait(false);
					return true;
				}
				catch (Exception ex)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"CreateEmbeddingsInBulk failed: {ex.Message}");
				}
			}

			return false;
		}

		public async Task<RegulationResponse> SubmitRegulationQuestion(RegulationLLMQuestion request)
		{
			RegulationResponse? generatedResponse = null;
			try
			{
				generatedResponse = await llmAgent.GenerateResponseAsync(request).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"LLM translation request failed: {ex.Message}");
			}

			if (generatedResponse == null)
			{
				generatedResponse = RegulationResponse.FailedResponse;
			}

			return generatedResponse;
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
				new ServiceReplicaListener(ctx => new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}
	}
}
