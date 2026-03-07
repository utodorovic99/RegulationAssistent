using System.Diagnostics;
using System.Fabric;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
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
					new FabricTransportServiceRemotingListener(ctx, this), "V2_1Listener")
			};
		}

		/// <summary>
		/// Store a document into a reliable dictionary. Delegates to DocumentPersistenceStorage.
		/// </summary>
		/// <param name="request">The document upload request containing the document metadata.</param>
		public async Task<DocumentItemDescriptor> StoreDocument(DocumentUploadRequest request)
		{
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
				}

				ServiceEventSource.Current.ServiceMessage(this.Context, $"StoreDocument called for: {request.Title}");
				var result = await persistence.StoreDocumentAsync(request);
				ServiceEventSource.Current.ServiceMessage(this.Context, $"StoreDocument completed successfully");
				return result;
			}
			catch (InvalidOperationException)
			{
				throw;
			}
			catch (ArgumentException)
			{
				throw;
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"StoreDocument failed: {ex.GetType().Name} - {ex.Message}");
				throw new InvalidOperationException($"Failed to store document: {ex.Message}", ex);
			}
		}

		/// <summary>
		/// Retrieves all stored documents. Delegates to DocumentPersistenceStorage.
		/// </summary>
		/// <returns>List of all document descriptors.</returns>
		public Task<List<DocumentItemDescriptor>> GetAllDocuments()
		{
			return persistence.GetAllDocumentsAsync();
		}

		/// <summary>
		/// Retrieves a specific document's bytes by title and version number.
		/// </summary>
		/// <param name="request">Request containing the document title and version number.</param>
		/// <returns>Response containing the document bytes.</returns>
		public async Task<GetDocumentResponse?> GetDocument(GetDocumentRequest request)
		{
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
				}

				ServiceEventSource.Current.ServiceMessage(this.Context, $"GetDocument called for: {request.Title} v{request.VersionNumber}");

				byte[]? fileBytes = await persistence.GetDocumentBytesAsync(request.Title, request.VersionNumber);

				if (fileBytes == null)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Document not found: {request.Title} v{request.VersionNumber}");
					return null;
				}

				ServiceEventSource.Current.ServiceMessage(this.Context, $"GetDocument completed successfully. Bytes: {fileBytes.Length}");

				return new GetDocumentResponse
				{
					FileBytes = fileBytes,
					Title = request.Title,
					VersionNumber = request.VersionNumber
				};
			}
			catch (InvalidOperationException)
			{
				throw;
			}
			catch (ArgumentException)
			{
				throw;
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"GetDocument failed: {ex.GetType().Name} - {ex.Message}");
				throw new InvalidOperationException($"Failed to retrieve document: {ex.Message}", ex);
			}
		}

		/// <summary>
		/// Deletes a specific document by title and version number.
		/// </summary>
		/// <param name="request">Request containing the document title and version number to delete.</param>
		/// <returns>Response indicating success or failure.</returns>
		public async Task<DeleteDocumentResponse> DeleteDocument(DeleteDocumentRequest request)
		{
			try
			{
				if (request == null)
				{
					throw new ArgumentNullException(nameof(request));
				}

				ServiceEventSource.Current.ServiceMessage(this.Context, $"DeleteDocument called for: {request.Title} v{request.VersionNumber}");

				bool deleted = await persistence.DeleteDocumentAsync(request.Title, request.VersionNumber);

				if (deleted)
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"DeleteDocument completed successfully");
					return new DeleteDocumentResponse { Success = true };
				}
				else
				{
					ServiceEventSource.Current.ServiceMessage(this.Context, $"Document not found: {request.Title} v{request.VersionNumber}");
					return new DeleteDocumentResponse
					{
						Success = false,
						ErrorMessage = "Document not found"
					};
				}
			}
			catch (InvalidOperationException ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"DeleteDocument failed: {ex.GetType().Name} - {ex.Message}");
				return new DeleteDocumentResponse
				{
					Success = false,
					ErrorMessage = ex.Message
				};
			}
			catch (ArgumentException ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"DeleteDocument failed: {ex.GetType().Name} - {ex.Message}");
				return new DeleteDocumentResponse
				{
					Success = false,
					ErrorMessage = ex.Message
				};
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"DeleteDocument failed: {ex.GetType().Name} - {ex.Message}");
				return new DeleteDocumentResponse
				{
					Success = false,
					ErrorMessage = $"Failed to delete document: {ex.Message}"
				};
			}
		}

		/// <summary>
		/// This is the main entry point for your service replica.
		/// This method executes when this replica of your service becomes primary and has write status.
		/// </summary>
		/// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
		protected override async Task RunAsync(CancellationToken cancellationToken)
		{
			try
			{
				await persistence.InitializeAsync();
			}
			catch (Exception ex)
			{
				ServiceEventSource.Current.ServiceMessage(this.Context, $"Failed to initialize document storage: {ex.Message}");
				throw;
			}
		}
	}
}