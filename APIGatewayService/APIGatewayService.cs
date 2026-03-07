using System.Fabric;
using APIGatewayService.Common.Listeners;
using APIGatewayService.Common.ServiceProxies;
using APIGatewayService.Context.Regulation.Documents;
using APIGatewayService.Context.Regulation.RegulationQuery.Requests;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace APIGatewayService
{
	/// <summary>
	/// Stateless Service Fabric service that hosts API gateway responsibilities.
	/// </summary>
	internal sealed class APIGatewayService : StatelessService
	{
		private readonly ServiceProxyPool serviceProxyPool;

		/// <summary>
		/// Creates a new instance of <see cref="APIGatewayService"/>.
		/// </summary>
		/// <param name="context">Service Fabric context provided by the runtime.</param>
		public APIGatewayService(StatelessServiceContext context)
			: base(context)
		{
			serviceProxyPool = new ServiceProxyPool();
		}

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return
			[
				new ServiceInstanceListener(serviceContext =>
					new HttpListenerWrapper(serviceContext,
						endpointName: "ServiceEndpoint",
						requestProcessors:
						[
							new RegulationQueryHttpRequestProcessor(serviceContext),
							new DocumentUploadHttpRequestProcessor(serviceContext, serviceProxyPool),
							new GetDocumentsHttpRequestProcessor(serviceContext, serviceProxyPool),
							new GetDocumentHttpRequestProcessor(serviceContext, serviceProxyPool),
							new DeleteDocumentHttpRequestProcessor(serviceContext, serviceProxyPool)
						]))
			];
		}
	}
}
