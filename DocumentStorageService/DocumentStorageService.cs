using System.Fabric;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Services;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace DocumentStorageService
{
	/// <summary>
	/// An instance of this class is created for each service replica by the Service Fabric runtime.
	/// </summary>
	internal sealed class DocumentStorageService : StatefulService, IDocumentStorageService
	{
		private readonly DocumentPersistenceStorage persistence;

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentStorageService"/> class.
		/// </summary>
		/// <param name="context">Service context.</param>
		public DocumentStorageService(StatefulServiceContext context)
			: base(context)
		{
			persistence = new DocumentPersistenceStorage(this.StateManager);
		}

		/// <summary>
		/// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
		/// </summary>
		/// <remarks>
		/// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
		/// </remarks>
		/// <returns>A collection of listeners.</returns>
		protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
		{
			return new ServiceReplicaListener[1]
			{
				new ServiceReplicaListener(ctx =>
					new FabricTransportServiceRemotingListener(ctx, this))
			};
		}

		/// <summary>
		/// Store a document into a reliable dictionary. Delegates to DocumentPersistenceStorage.
		/// </summary>
		/// <param name="request">The document upload request containing the document metadata.</param>
		public Task<DocumentItemDescriptor> StoreDocument(DocumentUploadRequest request)
		{
			return persistence.StoreDocumentAsync(request);
		}

		/// <summary>
		/// This is the main entry point for your service replica.
		/// This method executes when this replica of your service becomes primary and has write status.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			await persistence.InitializeAsync();
		}
	}
}