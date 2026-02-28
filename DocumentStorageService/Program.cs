using System.Diagnostics;
using Microsoft.ServiceFabric.Services.Runtime;

namespace DocumentStorageService
{
	/// <summary>
	/// Represents the entry point of the Document Storage Service, which is responsible for hosting the service and registering it with the Service Fabric runtime.
	/// </summary>
	internal static class Program
	{
		/// <summary>
		/// This is the entry point of the service host process.
		/// </summary>
		private static void Main()
		{
			try
			{
				// The ServiceManifest.XML file defines one or more service type names.
				// Registering a service maps a service type name to a .NET type.
				// When Service Fabric creates an instance of this service type,
				// an instance of the class is created in this host process.

				ServiceRuntime.RegisterServiceAsync("DocumentStorageServiceType",
					context => new DocumentStorageService(context)).GetAwaiter().GetResult();

				ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(DocumentStorageService).Name);

				// Prevents this host process from terminating so services keep running.
				Thread.Sleep(Timeout.Infinite);
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
				throw;
			}
		}
	}
}