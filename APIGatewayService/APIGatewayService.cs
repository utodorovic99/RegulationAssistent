using System.Fabric;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Context.Common;
using APIGatewayService.Context.Regulation.Documents;
using APIGatewayService.Context.Regulation.RegulationQuery.Requests;
using CommonSDK.ServiceProxies;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace APIGatewayService
{
	/// <summary>
	/// Stateless Service Fabric service that hosts API gateway responsibilities.
	/// </summary>
	internal sealed class APIGatewayService : StatelessService
	{
		private readonly IRpServiceProxyPool serviceProxyPool;

		/// <summary>
		/// Creates a new instance of <see cref="APIGatewayService"/>.
		/// </summary>
		/// <param name="context">Service Fabric context provided by the runtime.</param>
		public APIGatewayService(StatelessServiceContext context)
			: base(context)
		{
			serviceProxyPool = new RpServiceProxyPool();
		}

		protected override Task RunAsync(CancellationToken cancellationToken)
		{
			serviceProxyPool.RegisterFabricRP2Proxy<IDocumentStorageService>("fabric:/RegulationAssistent/DocumentStorageService", ServiceType.Stateful);
			serviceProxyPool.RegisterFabricRP2Proxy<IRegulationQuery>("fabric:/RegulationAssistent/QueryService", ServiceType.Stateless);

			return base.RunAsync(cancellationToken);
		}

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[1]
			{
				new ServiceInstanceListener(serviceContext =>
					new HttpListenerWrapper(
						serviceContext,
						endpointName: "ServiceEndpoint",
						requestProcessors: new IHttpRequestProcessor[]
						{
							new RegulationQueryHttpRequestProcessor(serviceContext, serviceProxyPool),
							new DocumentUploadHttpRequestProcessor(serviceContext, serviceProxyPool),
							new GetDocumentsHttpRequestProcessor(serviceContext, serviceProxyPool),
							new GetDocumentHttpRequestProcessor(serviceContext, serviceProxyPool),
							new DeleteDocumentHttpRequestProcessor(serviceContext, serviceProxyPool)
						}))
			};
		}
	}
}