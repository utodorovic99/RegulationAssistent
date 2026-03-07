using System;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace APIGatewayService.Common.ServiceProxies
{
	/// <summary>
	/// Pool that maintains shared instances of Service Fabric service proxies.
	/// This prevents creating multiple proxy instances for the same service and improves performance.
	/// </summary>
	internal class ServiceProxyPool
	{
		private readonly Lazy<IDocumentStorageService> documentStorageServiceProxy;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceProxyPool"/> class.
		/// </summary>
		public ServiceProxyPool()
		{
			documentStorageServiceProxy = new Lazy<IDocumentStorageService>(CreateDocumentStorageServiceProxy);
		}

		/// <summary>
		/// Gets the shared instance of the Document Storage Service proxy.
		/// </summary>
		public IDocumentStorageService DocumentStorageService => documentStorageServiceProxy.Value;

		/// <summary>
		/// Creates a proxy for calling document storage service.
		/// </summary>
		/// <returns>Proxy for calling document storage service.</returns>
		private static IDocumentStorageService CreateDocumentStorageServiceProxy()
		{
			var serviceUri = new Uri("fabric:/RegulationAssistent/DocumentStorageService");
			var proxyFactory = new ServiceProxyFactory((c) =>
				new FabricTransportServiceRemotingClientFactory());
			
			return proxyFactory.CreateServiceProxy<IDocumentStorageService>(
				serviceUri, 
				new ServicePartitionKey(0));
		}
	}
}
