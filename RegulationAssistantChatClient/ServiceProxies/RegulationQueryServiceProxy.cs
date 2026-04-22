using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using RegulationAssistantChatClient.Configuration;

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
		/// returns the deserialized <see cref="RegulationResponse"/>.
		/// </summary>
		/// <param name="request">The request to send. The method serializes this object to JSON.</param>
		/// <returns>
		/// The deserialized <see cref="RegulationResponse"/> when the call succeeds; otherwise
		/// a cached failure response (<see cref="FailedResponsesCached.FailedRegulationQueryResponse"/>)
		/// when an error occurs.
		/// </returns>
		/// <remarks>
		/// The method performs a POST to the configured service base URL using the path "Submit".
		/// Any exception during sending or deserialization is caught and results in returning the
		/// predefined failed response. Callers should treat the returned value accordingly.
		/// </remarks>
		public async Task<RegulationResponse> SendRegulationQueryAsync(RegulationQueryRequest request)
		{
			if (request == null)
			{
				return RegulationResponse.FailedResponse;
			}

			try
			{
				string requestUrl = "Submit";
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true,
				};

				HttpResponseMessage response = await httpClient.PostAsJsonAsync($"{serviceUrl}/{requestUrl}", request);
				response.EnsureSuccessStatusCode();

				RegulationResponse? deserializedResponse = await response.Content.ReadFromJsonAsync<RegulationResponse>(options);
				if (!string.IsNullOrWhiteSpace(deserializedResponse?.Answer))
				{
					return deserializedResponse;
				}
			}
			catch (Exception)
			{
			}

			return RegulationResponse.FailedResponse;
		}
	}
}