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
			serviceProxyPool.RegisterFabricRP2Proxy<IAuditService>("fabric:/RegulationAssistent/AuditService", ServiceType.Stateful);
			serviceProxyPool.RegisterFabricRP2Proxy<IDocumentIndexReader>("fabric:/RegulationAssistent/DocumentIndexingService", ServiceType.Stateful);
			serviceProxyPool.RegisterFabricRP2Proxy<ILLMService>("fabric:/RegulationAssistent/LLMService", ServiceType.Stateful);
		}

		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[1]
			{
				new ServiceInstanceListener(ctx =>
					new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}

		public async Task<RegulationResponse> SubmitQuestion(RegulationQueryRequest request)
		{
			string inProgressOperation = string.Empty;
			string serviceName = nameof(QueryService);

			var auditServiceProxy = serviceProxyPool.GetProxy<IAuditService>();
			long requestId = await auditServiceProxy.LogRegulationQueryRequestReceived(request.Question, request.Context);
			await auditServiceProxy.LogServiceEnter(requestId, serviceName);

			try
			{
				CreateEmbeddingRequest createQuestionEmbeddingRequest = new CreateEmbeddingRequest
				{
					RequestId = requestId,
					Text = request.Question,
				};

				inProgressOperation = "Request Embedding Creation";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				CreateEmbeddingResponse createQuestionEmbeddingResponse = await serviceProxyPool.GetProxy<ILLMService>().CreateEmbedding(createQuestionEmbeddingRequest).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				GetRelevantSectionsRequest getIndexedResultsRequest = new GetRelevantSectionsRequest()
				{
					RequestId = requestId,
					QuestionEmbedding = createQuestionEmbeddingResponse.Embedding,
					QuestionContext = request.Context,
					NumberOfResults = 3,
					ScoreThreshold = 0.5f,
				};

				inProgressOperation = "Search Index";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				GetRelevantSectionsResponse indexedResultsResponse = await serviceProxyPool.GetProxy<IDocumentIndexReader>().GetIndexedResults(getIndexedResultsRequest).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				AudibleRegulationLLMQuestion audibleRequest = new AudibleRegulationLLMQuestion
				{
					RequestId = requestId,
					Question = new RegulationLLMQuestion
					{
						RequestId = requestId,
						Question = request.Question,
						RelevantSections = indexedResultsResponse.RelevantSections,
					},
				};

				inProgressOperation = "Generate Response";
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Started");
				RegulationResponse llmResponse = await serviceProxyPool.GetProxy<ILLMService>().SubmitRegulationQuestion(audibleRequest.Question).ConfigureAwait(false);
				await auditServiceProxy.LogServiceEvent(requestId, serviceName, inProgressOperation, "Completed");

				await auditServiceProxy.LogRegulationQueryResponseReceived(requestId, serviceName, llmResponse.ShortAnswer, ResolveStatus(llmResponse), llmResponse.Confidence);
				return llmResponse;
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

		private static ResponseStatus ResolveStatus(RegulationResponse response)
		{
			if (response == null || string.IsNullOrWhiteSpace(response.ShortAnswer))
			{
				return ResponseStatus.Error;
			}

			if (response.Confidence <= 0)
			{
				return ResponseStatus.InsufficientData;
			}

			return response.Confidence < 0.5f
				? ResponseStatus.Partial
				: ResponseStatus.Successful;
		}
	}
}
