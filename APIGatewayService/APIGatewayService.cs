using System.Fabric;
using APIGatewayService.Common.Listeners;
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
		/// <summary>
		/// Creates a new instance of <see cref="APIGatewayService"/>.
		/// </summary>
		/// <param name="context">Service Fabric context provided by the runtime.</param>
		public APIGatewayService(StatelessServiceContext context)
			: base(context)
		{
		}

		/// <summary>
		/// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
		/// </summary>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
		{
			return new ServiceInstanceListener[2]
			{
				new ServiceInstanceListener(serviceContext =>
					new HttpListenerWrapper(serviceContext,
						endpointName: "ServiceEndpoint",
						apiPrefix: "RegulationQuery",
						[
							new RegulationQueryHttpRequestProcessor(serviceContext)
						])),

				new ServiceInstanceListener(serviceContext =>
					new HttpListenerWrapper(serviceContext,
						endpointName: "ServiceEndpoint",
						apiPrefix: "Documents",
						[
							new DocumentUploadHttpRequestProcessor(serviceContext)
						])),
			};
		}
	}
}