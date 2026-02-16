using APIGatewayService.Context.Regulation;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;

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
			return new[]
			{
				new ServiceInstanceListener(serviceContext =>
					new RegulationQueryHttpListener(serviceContext), "RegulationQueryHttpListener")
			};
		}

		/// <summary>
		/// The main run loop for the service instance. Called by Service Fabric and cancelled via the provided token.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token that is signaled when Service Fabric requests shutdown.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			// TODO: Replace the following sample code with your own logic
			//       or remove this RunAsync override if it's not needed in your service.

			long iterations = 0;

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();

				ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

				await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
			}
		}
	}
}