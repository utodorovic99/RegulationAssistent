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
		/// a cached failure response.
		/// when an error occurs.
		/// </returns>
		public async Task<RegulationResponse> SendRegulationQueryAsync(RegulationQueryRequest request)
		{
			if (request == null)
			{
				return RegulationResponse.CreateFailedResponse(0);
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
				if (!string.IsNullOrWhiteSpace(deserializedResponse?.ShortAnswer))
				{
					return deserializedResponse;
				}
			}
			catch (Exception)
			{
			}

			return RegulationResponse.CreateFailedResponse(0);
		}
	}
}