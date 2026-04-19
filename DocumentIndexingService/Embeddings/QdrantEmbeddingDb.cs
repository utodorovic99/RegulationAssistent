using System.Fabric;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonSDK.Converters;
using DocumentIndexingService.IndexingData;
using ExternalServiceContracts.Context.Regulation.Embeddings;
using ExternalServiceContracts.Context.Regulation.Embeddings.Responses;
using ExternalServiceContracts.Requests;

namespace DocumentIndexingService.Embeddings
{
	internal sealed class QdrantEmbeddingDb : IEmbeddingDb
	{
		private static readonly HttpClient httpClient = new HttpClient();
		private JsonSerializerOptions options;
		private EmbeddingDbConfiguration configuration;
		private readonly ICodePackageActivationContext? activationContext;

		public QdrantEmbeddingDb()
		{
			LoadConfiguration();
			InitializeConversionOptions();
		}

		public QdrantEmbeddingDb(ICodePackageActivationContext activationContext)
		{
			this.activationContext = activationContext;
			LoadConfiguration();
			InitializeConversionOptions();
		}

		public async Task StoreIndicesAsync(List<DocumentSectionIndex> entries)
		{
			if (entries == null || entries.Count == 0)
			{
				ServiceEventSource.Current.Message("StoreIndicesAsync: No entries to store.");
				return;
			}

			int successCount = 0;
			int failCount = 0;

			foreach (var entry in entries)
			{
				if (string.IsNullOrWhiteSpace(entry?.Id) || entry.Vector == null || entry.Vector.Length == 0)
				{
					ServiceEventSource.Current.Message($"StoreIndicesAsync: Skipping entry with invalid Id or Vector. Id: '{entry?.Id ?? "null"}'");
					failCount++;
					continue;
				}

				try
				{
					var qdrantId = GenerateUuidFromString(entry.Id);

					var body = new
					{
						points = new[]
						{
							new
							{
								id = qdrantId,
								vector = entry.Vector,
								payload = new
								{
									originalId = entry.Id,
									documentId = entry.Payload?.DocumentId,
									law = entry.Payload?.Law,
									chapter = entry.Payload?.Chapter,
									article = entry.Payload?.Article,
									validFrom = entry.Payload?.ValidFrom.ToString("yyyy-MM-dd"),
									validTo = entry.Payload?.ValidTo.ToString("yyyy-MM-dd"),
									text = entry.Payload?.Text
								}
							}
						}
					};

					string uri = $"{configuration.Url}/collections/{configuration.Collection}/points?wait=true";

					var json = JsonSerializer.Serialize(body, options);
					using var content = new StringContent(json, Encoding.UTF8, "application/json");
					var resp = await httpClient.PutAsync(uri, content).ConfigureAwait(false);

					var responseBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

					if (!resp.IsSuccessStatusCode)
					{
						ServiceEventSource.Current.Message($"StoreIndicesAsync: FAILED for Id '{entry.Id}' (UUID: {qdrantId}) - Status: {resp.StatusCode}, Response: {responseBody}");
						failCount++;
					}
					else
					{
						successCount++;
					}

					resp.EnsureSuccessStatusCode();
				}
				catch (Exception ex)
				{
					ServiceEventSource.Current.Message($"StoreIndicesAsync: Failed to store index Id '{entry.Id}'. Error: {ex.Message}");
					failCount++;
					throw;
				}
			}

			ServiceEventSource.Current.Message($"StoreIndicesAsync: Completed. Success: {successCount}, Failed: {failCount}");
		}

		public async Task DeleteIndices(string rootId)
		{
			if (string.IsNullOrEmpty(rootId)) return;

			var filter = GetFilterByDocumentId(rootId);

			string uri = $"{configuration.Url}/collections/{configuration.Collection}/points/delete?wait=true";

			var filterOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var json = JsonSerializer.Serialize(filter, filterOptions);
			using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
			var resp = await httpClient.PostAsync(uri, content).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
		}

		public async Task<GetRelevantSectionsResponse> GetIndexedResults(GetRelevantSectionsRequest request)
		{
			if (request == null)
			{
				return GetRelevantSectionsResponse.EmptyResponse;
			}

			var body = GetFilterByQuestionWithContext(request);

			string uri = $"{configuration.Url}/collections/{configuration.Collection}/points/search";

			var filterOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			var json = JsonSerializer.Serialize(body, filterOptions);
			using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
			var resp = await httpClient.PostAsync(uri, content).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();
			var respTxt = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

			// parse response for hits
			using var doc = JsonDocument.Parse(respTxt);
			var root = doc.RootElement;
			if (!root.TryGetProperty("result", out var resultEl) && !root.TryGetProperty("hits", out resultEl))
			{
				return GetRelevantSectionsResponse.EmptyResponse;
			}

			var resultsArray = resultEl.ValueKind == JsonValueKind.Array
				? resultEl.EnumerateArray()
				: resultEl.GetProperty("hits").EnumerateArray();

			GetRelevantSectionsResponse response = new GetRelevantSectionsResponse();
			foreach (var item in resultsArray)
			{
				var payloadEl = item.GetProperty("payload");

				response.RelevantSections.Add(new RelevantSection
				{
					Law = payloadEl.GetProperty("law").GetString(),
					Chapter = payloadEl.GetProperty("chapter").GetUInt32(),
					Article = payloadEl.GetProperty("article").GetUInt32(),
					Text = payloadEl.GetProperty("text").GetString(),
				});
			}

			return response;
		}

		private Object GetFilterByQuestionWithContext(GetRelevantSectionsRequest request)
		{
			return new
			{
				vector = request.QuestionEmbedding,
				top = request.NumberOfResults,
				filter = new
				{
					must = new object[]
					{
						new {
							key = "payload.validFrom",
							range = new { lte = request.QuestionContext.Date.ToString("yyyy-MM-dd") }
						},
						new {
							key = "payload.validTo",
							range = new { gte = request.QuestionContext.Date.ToString("yyyy-MM-dd") }
						}
					}
				}
			};
		}

		private Object GetFilterByDocumentId(string documentId)
		{
			return new
			{
				filter = new
				{
					must = new object[]
					{
						new {
							key = "documentId",
							match = new { value = documentId }
						}
					}
				}
			};
		}
		private void LoadConfiguration()
		{
			try
			{
				// Ensure we have an activation context (either provided or from Fabric runtime)
				var ctx = activationContext ?? FabricRuntime.GetActivationContext();
				var cfg = ctx.GetConfigurationPackageObject("Config");
				var section = cfg?.Settings?.Sections["EmbeddingDbConfig"];
				if (section == null)
				{
					throw new InvalidOperationException("EmbeddingDbConfig section not found in Config package settings.");
				}

				configuration = new EmbeddingDbConfiguration
				{
					Url = section.Parameters["Url"]?.Value,
					Collection = section.Parameters["Collection"]?.Value,
				};

				if (!configuration.IsValid)
				{
					throw new InvalidOperationException("EmbeddingDbConfig is invalid; 'Url' and 'Collection' must be provided in Settings.xml.");
				}
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.Write($"Failed to load EmbeddingDb configuration from Settings.xml: {e.Message}");
				throw;
			}
		}

		private void InitializeConversionOptions()
		{
			options = new JsonSerializerOptions
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			};

			options.Converters.Add(new DateOnlyJsonConverter());
		}

		private static string GenerateUuidFromString(string input)
		{
			using (var md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
				return new Guid(hash).ToString();
			}
		}
	}
}
