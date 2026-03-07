using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;
using ExternalServiceContracts.Context.Regulation.Documents.Responses;

namespace ExternalServiceContracts.Requests
{
	/// <summary>
	/// Represents the response returned after uploading a document to the API Gateway.
	/// </summary>
	[DataContract]
	public sealed class DocumentUploadResponse : ISerializableResponse
	{
		/// <summary>
		/// Gets or sets the descriptor of the uploaded document.
		/// </summary>
		[JsonPropertyName("result-descriptor")]
		[DataMember]
		public DocumentItemDescriptor DocumentDescriptor { get; set; }
	}
}