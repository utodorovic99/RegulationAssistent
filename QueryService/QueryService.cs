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

namespace QueryService
{
	internal sealed class QueryService : StatelessService, IRegulationQuery
	{
		private readonly IRpServiceProxyPool serviceProxyPool;

		public QueryService(StatelessServiceContext context)
				: base(context)
		{
			serviceProxyPool = new RpServiceProxyPool();
			serviceProxyPool.RegisterFabricRP2Proxy<IDocumentIndexReader>("fabric:/RegulationAssistent/DocumentIndexingService", ServiceType.Stateful);
			serviceProxyPool.RegisterFabricRP2Proxy<ILLMService>("fabric:/RegulationAssistent/LLMService", ServiceType.Stateful);
		}

		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[1]
			{
				new ServiceInstanceListener(ctx => new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}

		public async Task<RegulationResponse> SubmitQuestion(RegulationQueryRequest request)
		{
			ArgumentNullException.ThrowIfNull(request);

			if (request != null)
			{
				try
				{
					var createQuestionEmbeddingRequest = new CreateEmbeddingRequest { Text = request.Question };
					CreateEmbeddingResponse createQuestionEmbeddingResponse = await serviceProxyPool.GetProxy<ILLMService>()
						.CreateEmbedding(createQuestionEmbeddingRequest).ConfigureAwait(false);

					if (createQuestionEmbeddingResponse?.Embedding != null)
					{
						GetRelevantSectionsRequest getIndexedResultsRequest = new GetRelevantSectionsRequest()
						{
							QuestionEmbedding = createQuestionEmbeddingResponse.Embedding,
							QuestionContext = request.Context,
							NumberOfResults = 3,
							ScoreThreshold = 0.5f,
						};

						GetRelevantSectionsResponse indexedResultsResponse = await serviceProxyPool.GetProxy<IDocumentIndexReader>().GetIndexedResults(getIndexedResultsRequest).ConfigureAwait(false);
						if (indexedResultsResponse?.RelevantSections != null)
						{
							RegulationLLMQuestion llmQuestionRequest = new RegulationLLMQuestion()
							{
								Question = request.Question,
								RelevantSections = indexedResultsResponse.RelevantSections,
							};

							RegulationResponse llmResponse = await serviceProxyPool.GetProxy<ILLMService>().SubmitRegulationQuestion(llmQuestionRequest);
							if (llmResponse != null && !string.IsNullOrEmpty(llmResponse.ShortAnswer))
							{
								return llmResponse;
							}
						}
					}
				}
				catch (Exception e)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"SubmitQuestion on response service failed with exception: {e}");
				}
			}

			ServiceEventSource.Current.ServiceMessage(this.Context, $"SubmitQuestion on response service failed");

			return RegulationResponse.FailedResponse;
		}
	}
}
