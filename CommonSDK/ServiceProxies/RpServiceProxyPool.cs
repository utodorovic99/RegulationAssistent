using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace CommonSDK.ServiceProxies
{
	public enum ServiceType : short
	{
		Stateful = 0,
		Stateless = 1,
	}

	public class ServiceProxyDescriptor
	{
		public ServiceProxyDescriptor(Type contractType, string serviceUrl, ServiceType serviceType)
		{
			ContractType = contractType;
			ServiceUrl = serviceUrl;
			ServiceType = serviceType;
		}

		public Type ContractType { get; }
		public string ServiceUrl { get; }

		public ServiceType ServiceType { get; }

		public IService ServiceProxy { get; set; }
	}

	/// <summary>
	/// Pool that maintains shared instances of Service Fabric service proxies.
	/// This prevents creating multiple proxy instances for the same service and improves performance.
	/// </summary>
	public class RpServiceProxyPool : IRpServiceProxyPool
	{
		private readonly IDictionary<Type, ServiceProxyDescriptor> serviceProxyDescriptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="RpServiceProxyPool"/> class.
		/// </summary>
		public RpServiceProxyPool()
		{
			serviceProxyDescriptor = new Dictionary<Type, ServiceProxyDescriptor>(0);
		}

		public void RegisterFabricRP2Proxy<T>(string serviceUrl, ServiceType serviceType)
			where T : class, IService
		{
			Type contractType = typeof(T);

			if (!serviceProxyDescriptor.ContainsKey(contractType))
			{
				serviceProxyDescriptor[contractType] = new ServiceProxyDescriptor(contractType, serviceUrl, serviceType);
			}
		}

		public T GetProxy<T>()
			where T : class, IService
		{
			Type contractType = typeof(T);

			if (!serviceProxyDescriptor.TryGetValue(contractType, out ServiceProxyDescriptor proxyDescriptor)
					|| proxyDescriptor == null)
			{
				throw new InvalidOperationException($"Contract type {contractType.FullName} is not registered.");
			}


			if (proxyDescriptor.ServiceProxy == null)
			{
				//First call or it was called before target service was initialized.
				proxyDescriptor.ServiceProxy = CreateFabricRP2Proxy<T>(proxyDescriptor);
			}

			if (proxyDescriptor.ServiceProxy == null)
			{
				throw new InvalidOperationException($"Failed to create service proxy for contract type {contractType.FullName}.");
			}

			return (T)proxyDescriptor.ServiceProxy;
		}

		private T CreateFabricRP2Proxy<T>(ServiceProxyDescriptor proxyDescriptor)
			where T : class, IService
		{
			Type contractType = typeof(T);
			var serviceUri = new Uri(proxyDescriptor.ServiceUrl);
			var proxyFactory = new ServiceProxyFactory((c) => new FabricTransportServiceRemotingClientFactory());

			return proxyDescriptor.ServiceType == ServiceType.Stateful
				? proxyFactory.CreateServiceProxy<T>(serviceUri, new ServicePartitionKey(0))
				: proxyFactory.CreateServiceProxy<T>(serviceUri, ServicePartitionKey.Singleton);
		}
	}
}