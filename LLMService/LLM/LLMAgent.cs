using System.Fabric;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExternalServiceContracts.Requests;
using LLMService;

namespace ResponseService
{
	internal sealed class LLMAgent : ILLMAgent
	{
		private readonly HttpClient http;
		private LLMConfiguration configuration;
		private readonly ICodePackageActivationContext? activationContext;

		// Accept an optional activation context so the agent can locate files in the Config package when running in Service Fabric
		public LLMAgent()
		{
			http = new HttpClient();
			InitializeConfiguration();
		}

		public LLMAgent(ICodePackageActivationContext activationContext)
		{
			this.activationContext = activationContext;
			http = new HttpClient();
			InitializeConfiguration();
		}

		public async Task<float[][]> CreateEmbeddingsAsync(string[] texts)
		{
			if (texts == null || texts.Length == 0)
			{
				return Array.Empty<float[]>();
			}

			try
			{
				var payload = new
				{
					model = configuration.EmbeddingsModel,
					input = texts,
				};

				var request = new HttpRequestMessage(HttpMethod.Post, configuration.EmbeddingsPath);
				request.Content = JsonContent.Create(payload);

				var resp = await http.SendAsync(request).ConfigureAwait(false);

				if (!resp.IsSuccessStatusCode)
				{
					string respBody = string.Empty;
					try { respBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false); } catch { }
					ServiceEventSource.Current.Message($"LLMAgent: Embeddings request failed. Url={resp.RequestMessage?.RequestUri}, Status={(int)resp.StatusCode} {resp.ReasonPhrase}, Body={respBody}");
					return Array.Empty<float[]>();
				}

				var json = await resp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
				if (!json.TryGetProperty("embeddings", out var embeddingsData))
				{
					return Array.Empty<float[]>();
				}

				var list = new List<float[]>();
				foreach (var emb in embeddingsData.EnumerateArray())
				{
					var floats = emb.EnumerateArray().Select(e => (float)e.GetDouble()).ToArray();
					list.Add(floats);
				}

				return list.ToArray();
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.Message($"LLMAgent: CreateEmbeddingsAsync exception: {e.Message}");
				return Array.Empty<float[]>();
			}
		}

		public async Task<float[]> CreateEmbeddingAsync(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return Array.Empty<float>();
			}

			var embeddings = await CreateEmbeddingsAsync(new[] { text }).ConfigureAwait(false);
			if (embeddings == null || embeddings.Length == 0)
			{
				return Array.Empty<float>();
			}

			return embeddings[0] ?? Array.Empty<float>();
		}

		public async Task<string> GenerateResponseAsync(RegulationLLMQuestion request)
		{
			string response = string.Empty;
			try
			{
				if (string.IsNullOrEmpty(configuration.CompletionsPath) || string.IsNullOrEmpty(configuration.CompletionsModel))
				{
					return "ERROR";
				}

				if (request == null || !request.IsValid())
				{
					response = await InvalidQuestionFastReponse(request).ConfigureAwait(false);
				}
				else
				{
					response = await GetDetailedResponse(request).ConfigureAwait(false);
				}
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.Message($"LLMAgent: GenerateResponseAsync failed with: {e.Message}");
			}

			if (string.IsNullOrEmpty(response))
			{
				response = MessageConsts.FailedToCreateResponseErr;
			}

			return response?.Trim() ?? string.Empty;
		}

		private async Task<string> GetDetailedResponse(RegulationLLMQuestion request)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Relevant sections (use ONLY these when answering):");
			if (request.RelevantSections != null && request.RelevantSections.Count > 0)
			{
				int idx = 1;
				foreach (var sec in request.RelevantSections)
				{
					sb.AppendLine($"--- Section #{idx} ---");
					if (!string.IsNullOrEmpty(sec.Law)) sb.AppendLine($"Law: {sec.Law}");
					if (sec.Chapter != 0) sb.AppendLine($"Chapter: {sec.Chapter}");
					if (sec.Article != 0) sb.AppendLine($"Article: {sec.Article}");
					if (!string.IsNullOrEmpty(sec.Text)) sb.AppendLine($"Text: {sec.Text}");
					idx++;
				}
			}
			else
			{
				sb.AppendLine("<NO RELEVANT SECTIONS PROVIDED>");
			}

			string systemInstruction =
				"You are a helpful assistant. Use ONLY the provided relevant sections. Do NOT use any outside knowledge or assumptions.\n" +
				"Produce exactly one plain-text response and nothing else (no JSON, no code fences, no extra commentary). The response MUST follow this exact structure and labels:\n\n" +
				"Short Answer: <concise answer in the SAME LANGUAGE as the question>\n" +
				"Explanation: <short explanation in the SAME LANGUAGE, derived STRICTLY from the provided sections; if insufficient information, state that explicitly>\n" +
				"Citations:\n" +
				"- document-name: <name>, document-version: <version>, document-section: <section identifier or index>, citation-content: \"<exact excerpt from provided sections>\"\n" +
				"(Repeat the dash-prefixed line for each citation. If there are no citations, write 'None')\n" +
				"Confidence: <number between 0 and 1>\n\n" +
				"Constraints:\n" +
				"1) Use ONLY the provided relevant sections. If the sections do NOT contain enough information to answer the question, set Short Answer to 'Insufficient information to answer' (in the same language as the question), Explanation must state that explicitly, Citations should be 'None' and Confidence should be 0.\n" +
				"2) Reply in the same language as the question.\n" +
				"3) Return plain text following the exact labels and structure above.\n";

			string userPrompt = "Question:\n" + request.Question + "\n\n" + sb.ToString();

			var payload = new
			{
				model = configuration.CompletionsModel,
				messages = new[]
				{
					new { role = "system", content = systemInstruction },
					new { role = "user", content = userPrompt }
				},
				max_tokens = 800,
				temperature = 0.0
			};

			var httpReq = new HttpRequestMessage(HttpMethod.Post, configuration.CompletionsPath);
			httpReq.Content = JsonContent.Create(payload);

			var resp = await http.SendAsync(httpReq).ConfigureAwait(false);
			resp.EnsureSuccessStatusCode();

			var json = await resp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
			if (json.TryGetProperty("choices", out var choices))
			{
				var first = choices.EnumerateArray().FirstOrDefault();
				if (first.ValueKind != JsonValueKind.Undefined)
				{
					if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
					{
						return content.GetString();
					}
					if (first.TryGetProperty("text", out var text))
					{
						return text.GetString();
					}
				}
			}

			return string.Empty;
		}

		private async Task<string> InvalidQuestionFastReponse(RegulationLLMQuestion request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Question))
			{
				return MessageConsts.FailedToCreateResponseNoInfo;
			}

			string translateSystem = "You are a precise translator. Translate the following short message into the SAME LANGUAGE as the example sentence. Return only the translated message and nothing else (no commentary, no extra punctuation).";
			string translateUser = $"Example sentence: {request.Question}\n\nMessage to translate: {MessageConsts.FailedToCreateResponseNoInfo}";

			var translatePayload = new
			{
				model = configuration.CompletionsModel,
				messages = new[]
				{
					new { role = "system", content = translateSystem },
					new { role = "user", content = translateUser }
				},
				max_tokens = 60,
				temperature = 0.0
			};

			var httpReq = new HttpRequestMessage(HttpMethod.Post, configuration.CompletionsPath);
			httpReq.Content = JsonContent.Create(translatePayload);

			var respTrans = await http.SendAsync(httpReq).ConfigureAwait(false);
			respTrans.EnsureSuccessStatusCode();

			var jsonTrans = await respTrans.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
			if (jsonTrans.TryGetProperty("choices", out var choicesTrans))
			{
				var firstTrans = choicesTrans.EnumerateArray().FirstOrDefault();
				if (firstTrans.ValueKind != JsonValueKind.Undefined)
				{
					if (firstTrans.TryGetProperty("message", out var messageTrans) && messageTrans.TryGetProperty("content", out var contentTrans))
					{
						return contentTrans.GetString();
					}
					if (firstTrans.TryGetProperty("text", out var textTrans))
					{
						return textTrans.GetString();
					}
				}
			}

			return string.Empty;
		}

		private void InitializeConfiguration()
		{
			try
			{
				ICodePackageActivationContext ctx = activationContext ?? FabricRuntime.GetActivationContext();
				var cfg = ctx.GetConfigurationPackageObject("Config");
				var section = cfg?.Settings?.Sections["LLMConfig"];
				if (section == null)
				{
					throw new InvalidOperationException("LLMConfig section not found in Config package settings.");
				}

				configuration = new LLMConfiguration
				{
					EmbeddingsPath = section.Parameters["EmbeddingsPath"]?.Value,
					EmbeddingsModel = section.Parameters["EmbeddingsModel"]?.Value,
					CompletionsPath = section.Parameters["CompletionsPath"]?.Value,
					CompletionsModel = section.Parameters["CompletionsModel"]?.Value,
				};

				if (!configuration.IsValid)
				{
					throw new InvalidOperationException("LLMConfig is invalid; ensure 'APIKey', 'EmbeddingsPath' and 'EmbeddingsModel' are set in Settings.xml.");
				}
			}
			catch (Exception e)
			{
				ServiceEventSource.Current.Message($"LLMAgent: Failed to load LLM configuration from Settings.xml: {e.Message}");
				throw;
			}
		}
	}
}
