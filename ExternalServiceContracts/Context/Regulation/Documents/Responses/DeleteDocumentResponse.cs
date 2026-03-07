using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using CommonSDK;

namespace ExternalServiceContracts.Context.Regulation.Documents.Responses
{
	/// <summary>
	/// Response indicating whether a document was successfully deleted.
	/// </summary>
	[DataContract]
	public sealed class DeleteDocumentResponse : ISerializableResponse
	{
		/// <summary>
		/// Gets or sets a value indicating whether the deletion was successful.
		/// </summary>
		[JsonPropertyName("success")]
		[DataMember]
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets an error message if the deletion failed.
		/// </summary>
		[JsonPropertyName("errorMessage")]
		[DataMember]
		public string? ErrorMessage { get; set; }
	}
}
