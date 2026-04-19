using Microsoft.ServiceFabric.Services.Remoting;

namespace CommonSDK.ServiceProxies
{
	public interface IRpServiceProxyPool
	{
		void RegisterFabricRP2Proxy<T>(string serviceUrl, ServiceType serviceType)
			where T : class, IService;

		T GetProxy<T>()
			where T : class, IService;
	}
}