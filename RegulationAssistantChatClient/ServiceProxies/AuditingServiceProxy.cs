using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ExternalServiceContracts.Responses;
using RegulationAssistantChatClient.Configuration;

namespace RegulationAssistantChatClient.Services
{
	/// <summary>
	/// Proxy used to send regulation query auditing requests to the audit service endpoint.
	/// Encapsulates HTTP interactions and deserialization of the response.
	/// </summary>
	public class AuditingServiceProxy
	{
		private readonly string serviceUrl;
		private readonly HttpClient httpClient;

		/// <summary>
		/// Creates a new instance of <see cref="AuditingServiceProxy"/>.
		/// </summary>
		/// <param name="httpClient">An <see cref="HttpClient"/> instance used to perform HTTP requests. Must not be null.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="httpClient"/> is null.</exception>
		public AuditingServiceProxy(HttpClient httpClient)
		{
			this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			serviceUrl = AppSettingsProvider.Settings.AuditingServiceBaseUrl;
		}

		/// <summary>
		/// Sends request to trace the audit service and returns the deserialized <see cref="RegulationQueryTraceResponse"/>.
		/// </summary>
		/// <returns><see cref="RegulationQueryTraceResponse"/> for last request.</returns>
		public async Task<RegulationQueryTraceResponse> TraceLastRegulationQueryAsync()
		{
			try
			{
				string requestUrl = "TraceLastRequest";
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true,
				};

				HttpResponseMessage response = await httpClient.PostAsync($"{serviceUrl}/{requestUrl}", null);
				response.EnsureSuccessStatusCode();

				RegulationQueryTraceResponse? deserializedResponse = await response.Content.ReadFromJsonAsync<RegulationQueryTraceResponse>(options);
				if (!string.IsNullOrWhiteSpace(deserializedResponse?.Trace))
				{
					return deserializedResponse;
				}
			}
			catch (Exception)
			{
			}

			return new RegulationQueryTraceResponse() { Trace = "UNKNOWN" };
		}
	}
}