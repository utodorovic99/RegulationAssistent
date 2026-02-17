using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;
using RegulationAssistantChatClient.Configuration;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RegulationAssistantChatClient.Services
{
	/// <summary>
	/// Proxy used to send regulation query requests to the Regulation Query service endpoint.
	/// Encapsulates HTTP interactions and deserialization of the response.
	/// </summary>
	public class RegulationQueryServiceProxy
	{
		private readonly string serviceUrl;
		private readonly HttpClient httpClient;

		/// <summary>
		/// Creates a new instance of <see cref="RegulationQueryServiceProxy"/>.
		/// </summary>
		/// <param name="httpClient">An <see cref="HttpClient"/> instance used to perform HTTP requests. Must not be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
		public RegulationQueryServiceProxy(HttpClient httpClient)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			serviceUrl = AppSettingsProvider.Settings.RegulationQueryServiceBaseUrl;
		}

		/// <summary>
		/// Sends a <see cref="RegulationQueryRequest"/> to the remote Regulation Query service and
		/// returns the deserialized <see cref="RegulationQueryResponse"/>.
		/// </summary>
		/// <param name="request">The request to send. The method serializes this object to JSON.</param>
		/// <returns>
		/// The deserialized <see cref="RegulationQueryResponse"/> when the call succeeds; otherwise
		/// a cached failure response (<see cref="FailedResponsesCached.FailedRegulationQueryResponse"/>)
		/// when an error occurs.
		/// </returns>
		/// <remarks>
		/// The method performs a POST to the configured service base URL using the path "Submit".
		/// Any exception during sending or deserialization is caught and results in returning the
		/// predefined failed response. Callers should treat the returned value accordingly.
		/// </remarks>
		public async Task<RegulationQueryResponse> SendRegulationQueryAsync(RegulationQueryRequest request)
		{
			try
			{
				string requestUrl = "Submit";

				string jsonContent = JsonSerializer.Serialize(request);
				HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await httpClient.PostAsync($"{serviceUrl}/{requestUrl}", httpContent);
				response.EnsureSuccessStatusCode();

				string responseContent = await response.Content.ReadAsStringAsync();

				RegulationQueryResponse deserializedResponse = JsonSerializer.Deserialize<RegulationQueryResponse>(responseContent);
				if (deserializedResponse != null)
				{
					return deserializedResponse;
				}
			}
			catch (Exception e)
			{
			}

			return FailedResponsesCached.FailedRegulationQueryResponse;
		}
	}
}