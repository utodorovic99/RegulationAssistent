using System.Threading.Tasks;
using ExternalServiceContracts.Context.Regulation.Documents.Requests;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;
using Microsoft.ServiceFabric.Services.Remoting;

namespace ExternalServiceContracts.Services
{
	/// <summary>
	/// Contract for document indexing write operations.
	/// </summary>
	public interface IDocumentIndexWritter : IService
	{
		Task<bool> BuildDocumentIndex(BuildDocumentIndexRequest request);

		Task<bool> RemoveDocumentIndex(DocumentItemDescriptor document);
	}
}