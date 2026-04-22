using System.Fabric;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Globalization;
using ExternalServiceContracts.Common;
using ExternalServiceContracts.Requests;
using ExternalServiceContracts.Responses;
using LLMService;

namespace ResponseService
{
	internal sealed class LLMAgent : ILLMAgent
	{
		private static readonly TimeSpan HttpTimeout = TimeSpan.FromMinutes(10);
		private static readonly ResponseLabelLocalizationCache labelLocalizationCache = new ResponseLabelLocalizationCache();
		private readonly HttpClient http;
		private LLMConfiguration configuration;
		private readonly ICodePackageActivationContext? activationContext;

		// Accept an optional activation context so the agent can locate files in the Config package when running in Service Fabric
		public LLMAgent()
		{
			http = new HttpClient();
			http.Timeout = HttpTimeout;
			InitializeConfiguration();
		}

		public LLMAgent(ICodePackageActivationContext activationContext)
		{
			this.activationContext = activationContext;
			http = new HttpClient();
			http.Timeout = HttpTimeout;
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

		public async Task<RegulationResponse> GenerateResponseAsync(RegulationLLMQuestion request)
		{
			RegulationResponse? response = null;
			try
			{
				if (string.IsNullOrEmpty(configuration.CompletionsPath) || string.IsNullOrEmpty(configuration.CompletionsModel))
				{
					return CreateFallbackResponse(MessageConsts.FailedToCreateResponseErr);
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

			if (response == null || string.IsNullOrWhiteSpace(response.ShortAnswer))
			{
				response = CreateFallbackResponse(MessageConsts.FailedToCreateResponseErr);
			}

			response.Answer = response.Answer?.Trim() ?? string.Empty;
			response.ShortAnswer = response.ShortAnswer?.Trim() ?? string.Empty;
			response.Explanation = response.Explanation?.Trim() ?? string.Empty;
			response.Citations ??= new List<DocumentCitation>();
			return response;
		}

		private async Task<RegulationResponse> GetDetailedResponse(RegulationLLMQuestion request)
		{
			string detectedLanguage = await DetectLanguageCodeAsync(request.Question).ConfigureAwait(false);

			var sb = new StringBuilder();
			sb.AppendLine("Context sections:");
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
				"You are an AI assistant that answers strictly based on the provided context.\n\n" +
				"RULES:\n" +
				"- Answer ONLY using the provided document sections\n" +
				"- If the answer is not found, say so\n" +
				"- DO NOT hallucinate\n" +
				"- Citations MUST be exact (no paraphrasing)\n\n" +
				"LANGUAGE:\n" +
				"- Target answer language code is provided by the application\n" +
				"- short_answer and explanation MUST be in that target language\n" +
				"- Do NOT copy language from context if it differs from question language\n" +
				"- If needed, translate your final short_answer and explanation to question language before returning JSON\n" +
				"- Citations must remain in original language\n\n" +
				"OUTPUT FORMAT (STRICT JSON ONLY):\n" +
				"{\n" +
				"  \"short_answer\": \"...\",\n" +
				"  \"explanation\": \"...\",\n" +
				"  \"citations\": [\n" +
				"    {\n" +
				"      \"document\": \"...\",\n" +
				"      \"version\": \"...\",\n" +
				"      \"section\": \"...\",\n" +
				"      \"content\": \"...\"\n" +
				"    }\n" +
				"  ],\n" +
				"  \"confidence\": 0.0\n" +
				"}";

			string userPrompt = $"Target language code: {detectedLanguage}\n\nQuestion:\n{request.Question}\n\n{sb}";
			string completionText = await RequestCompletionTextAsync(systemInstruction, userPrompt, 800).ConfigureAwait(false);
			if (TryParseMarkedResponse(completionText, out var parsed))
			{
				parsed = await EnsureResponseLanguageAsync(parsed, detectedLanguage).ConfigureAwait(false);
				var localizedLabels = await GetLocalizedLabelsAsync(detectedLanguage).ConfigureAwait(false);
				parsed.Answer = BuildFormattedAnswer(parsed, localizedLabels);
				return parsed;
			}

			ServiceEventSource.Current.Message($"LLMAgent: Could not parse structured LLM response. RawText={completionText}");
			return new RegulationResponse
			{
				Answer = completionText?.Trim() ?? MessageConsts.FailedToCreateResponseErr,
				ShortAnswer = completionText?.Trim() ?? MessageConsts.FailedToCreateResponseErr,
				Explanation = string.Empty,
				Citations = new List<DocumentCitation>(),
				Confidence = 0,
			};
		}

		private async Task<RegulationResponse> InvalidQuestionFastReponse(RegulationLLMQuestion request)
		{
			if (request == null || string.IsNullOrWhiteSpace(request.Question))
			{
				return CreateFallbackResponse(MessageConsts.FailedToCreateResponseNoInfo);
			}

			string translateSystem = "You are a precise translator. Translate the following short message into the SAME LANGUAGE as the example sentence. Return only the translated message and nothing else (no commentary, no extra punctuation).";
			string translateUser = $"Example sentence: {request.Question}\n\nMessage to translate: {MessageConsts.FailedToCreateResponseNoInfo}";

			string translated = await RequestCompletionTextAsync(translateSystem, translateUser, 60).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(translated))
			{
				translated = MessageConsts.FailedToCreateResponseNoInfo;
			}

			return CreateFallbackResponse(translated);
		}

		private async Task<string> RequestCompletionTextAsync(string systemPrompt, string userPrompt, int maxTokens)
		{
			if (string.IsNullOrWhiteSpace(configuration.OpenAIApiKey))
			{
				ServiceEventSource.Current.Message("LLMAgent: OpenAI API key is missing. Set LLMConfig/APIKey.");
				return string.Empty;
			}

			var openAiPayload = new
			{
				model = configuration.CompletionsModel,
				messages = new[]
				{
					new { role = "system", content = systemPrompt },
					new { role = "user", content = userPrompt }
				},
				max_tokens = maxTokens,
				temperature = 0.0,
				stream = false
			};

			var completionResponse = await SendJsonAsync(configuration.CompletionsPath, openAiPayload, includeBearerAuth: true).ConfigureAwait(false);
			if (!completionResponse.ok)
			{
				ServiceEventSource.Current.Message($"LLMAgent: OpenAI completion request failed. Url={configuration.CompletionsPath}, Status={completionResponse.statusCode}, Body={completionResponse.rawBody}");
				return string.Empty;
			}

			return completionResponse.text;
		}

		private async Task<RegulationResponse> EnsureResponseLanguageAsync(RegulationResponse response, string targetLanguage)
		{
			if (string.IsNullOrWhiteSpace(response.ShortAnswer) && string.IsNullOrWhiteSpace(response.Explanation))
			{
				return response;
			}

			string sample = !string.IsNullOrWhiteSpace(response.ShortAnswer) ? response.ShortAnswer : response.Explanation;
			string currentLanguage = await DetectLanguageCodeAsync(sample).ConfigureAwait(false);
			if (string.Equals(currentLanguage, targetLanguage, StringComparison.OrdinalIgnoreCase))
			{
				return response;
			}

			string translatePrompt =
				"Translate the following two fields to the target language and return STRICT JSON only.\n" +
				"Do not translate citations.\n" +
				"Schema:\n" +
				"{\n" +
				"  \"short_answer\": \"...\",\n" +
				"  \"explanation\": \"...\"\n" +
				"}\n\n" +
				$"Target language code: {targetLanguage}\n" +
				$"short_answer: {response.ShortAnswer}\n" +
				$"explanation: {response.Explanation}";

			string translated = await RequestCompletionTextAsync("You are a precise translator.", translatePrompt, 180).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(translated))
			{
				return response;
			}

			try
			{
				using var doc = JsonDocument.Parse(NormalizeCompletionText(translated));
				var root = doc.RootElement;
				if (root.ValueKind != JsonValueKind.Object)
				{
					return response;
				}

				string shortAnswer = ReadJsonString(root, "short_answer");
				string explanation = ReadJsonString(root, "explanation");
				if (!string.IsNullOrWhiteSpace(shortAnswer)) response.ShortAnswer = shortAnswer;
				if (!string.IsNullOrWhiteSpace(explanation)) response.Explanation = explanation;
			}
			catch
			{
				return response;
			}

			return response;
		}

		private static RegulationResponse CreateFallbackResponse(string message)
		{
			return new RegulationResponse
			{
				Answer = message,
				ShortAnswer = message,
				Explanation = string.Empty,
				Citations = new List<DocumentCitation>(),
				Confidence = 0,
			};
		}

		private async Task<(bool ok, int statusCode, string text, string rawBody)> SendJsonAsync(string url, object payload, bool includeBearerAuth = false)
		{
			using var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = JsonContent.Create(payload)
			};

			if (includeBearerAuth)
			{
				httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.OpenAIApiKey);
			}

			HttpResponseMessage resp;
			try
			{
				resp = await http.SendAsync(httpReq).ConfigureAwait(false);
			}
			catch (TaskCanceledException ex)
			{
				ServiceEventSource.Current.Message($"LLMAgent: HTTP timeout calling LLM endpoint. Url={url}, TimeoutSeconds={http.Timeout.TotalSeconds}, Error={ex.Message}");
				return (false, 408, string.Empty, "Request timeout while waiting for LLM response.");
			}
			catch (HttpRequestException ex)
			{
				ServiceEventSource.Current.Message($"LLMAgent: HTTP request failed. Url={url}, Error={ex.Message}");
				return (false, 503, string.Empty, ex.Message);
			}

			using (resp)
			{
				string rawBody = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!resp.IsSuccessStatusCode)
				{
					return (false, (int)resp.StatusCode, string.Empty, rawBody);
				}

				if (!TryExtractCompletionText(rawBody, out var text))
				{
					ServiceEventSource.Current.Message($"LLMAgent: Completion response parsing failed. Url={url}, Body={rawBody}");
					return (false, (int)resp.StatusCode, string.Empty, rawBody);
				}

				return (true, (int)resp.StatusCode, text, rawBody);
			}
		}

		private static bool TryExtractCompletionText(string rawBody, out string text)
		{
			text = string.Empty;
			if (string.IsNullOrWhiteSpace(rawBody))
			{
				return false;
			}

			using var doc = JsonDocument.Parse(rawBody);
			var json = doc.RootElement;

			if (json.TryGetProperty("choices", out var choices))
			{
				var first = choices.EnumerateArray().FirstOrDefault();
				if (first.ValueKind != JsonValueKind.Undefined)
				{
					if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var content))
					{
						text = content.GetString() ?? string.Empty;
						return !string.IsNullOrWhiteSpace(text);
					}
					if (first.TryGetProperty("text", out var choiceText))
					{
						text = choiceText.GetString() ?? string.Empty;
						return !string.IsNullOrWhiteSpace(text);
					}
				}
			}

			if (json.TryGetProperty("message", out var ollamaMessage) && ollamaMessage.TryGetProperty("content", out var ollamaContent))
			{
				text = ollamaContent.GetString() ?? string.Empty;
				return !string.IsNullOrWhiteSpace(text);
			}

			if (json.TryGetProperty("response", out var ollamaResponse))
			{
				text = ollamaResponse.GetString() ?? string.Empty;
				return !string.IsNullOrWhiteSpace(text);
			}

			return false;
		}

		private static bool TryParseMarkedResponse(string rawCompletionText, out RegulationResponse response)
		{
			response = CreateFallbackResponse(string.Empty);
			if (string.IsNullOrWhiteSpace(rawCompletionText))
			{
				return false;
			}

			string normalized = NormalizeCompletionText(rawCompletionText);
			using var doc = JsonDocument.Parse(normalized);
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return false;
			}

			string shortAnswer = ReadJsonString(root, "short_answer");
			string explanation = ReadJsonString(root, "explanation");
			float confidence = 0;
			if (root.TryGetProperty("confidence", out var confidenceEl))
			{
				if (confidenceEl.ValueKind == JsonValueKind.Number && confidenceEl.TryGetSingle(out var confidenceNumber))
				{
					confidence = confidenceNumber;
				}
				else if (confidenceEl.ValueKind == JsonValueKind.String)
				{
					float.TryParse(confidenceEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out confidence);
				}
			}

			var citations = new List<DocumentCitation>();
			if (root.TryGetProperty("citations", out var citationsEl) && citationsEl.ValueKind == JsonValueKind.Array)
			{
				foreach (var c in citationsEl.EnumerateArray())
				{
					if (c.ValueKind != JsonValueKind.Object)
					{
						continue;
					}

					citations.Add(new DocumentCitation
					{
						DocumentName = ReadJsonString(c, "document", "document-name", "documentName"),
						Version = ReadJsonString(c, "version", "document-version", "documentVersion"),
						SectionId = ReadJsonString(c, "section", "document-section", "documentSection"),
						Citation = ReadJsonString(c, "content", "citation-content", "citationContent")
					});
				}
			}

			if (string.IsNullOrWhiteSpace(shortAnswer) && string.IsNullOrWhiteSpace(explanation) && citations.Count == 0)
			{
				return false;
			}

			response = new RegulationResponse
			{
				Answer = string.Empty,
				ShortAnswer = shortAnswer,
				Explanation = explanation,
				Citations = citations,
				Confidence = confidence,
			};

			return !string.IsNullOrWhiteSpace(response.ShortAnswer);
		}

		private static string NormalizeCompletionText(string value)
		{
			string text = value.Trim();
			if (text.StartsWith("```", StringComparison.Ordinal))
			{
				int firstNewLine = text.IndexOf('\n');
				if (firstNewLine >= 0)
				{
					text = text[(firstNewLine + 1)..];
				}

				if (text.EndsWith("```", StringComparison.Ordinal))
				{
					text = text[..^3];
				}
			}

			return text.Trim();
		}

		private static string ReadJsonString(JsonElement element, params string[] propertyNames)
		{
			foreach (var propertyName in propertyNames)
			{
				if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
				{
					return prop.GetString() ?? string.Empty;
				}
			}

			return string.Empty;
		}

		private async Task<ResponseSectionLabels> GetLocalizedLabelsAsync(string detectedLanguage)
		{
			if (labelLocalizationCache.TryGet(detectedLanguage, out var labels))
			{
				return labels;
			}

			var fetched = await FetchLabelsFromLlmAsync(detectedLanguage).ConfigureAwait(false);
			if (fetched.HasValue)
			{
				labelLocalizationCache.Set(detectedLanguage, fetched.Value);
				return fetched.Value;
			}

			return labelLocalizationCache.GetEnglishFallback();
		}

		private async Task<string> DetectLanguageCodeAsync(string question)
		{
			if (string.IsNullOrWhiteSpace(question))
			{
				return "en";
			}

			string prompt =
				"Detect the language of the provided text and return ONLY ISO 639-1 language code in lowercase (example: en, sr, de). No explanation.\n\n" +
				$"Text: {question}";

			string result = await RequestCompletionTextAsync("You are a language detector.", prompt, 10).ConfigureAwait(false);
			string normalized = (result ?? string.Empty).Trim().ToLowerInvariant();
			if (normalized.Length >= 2)
			{
				normalized = normalized[..2];
			}

			if (normalized is "en" or "sr" or "de")
			{
				return normalized;
			}

			if (question.Any(c => c is '?' or '?' or 'ž' or 'š' or '?' || (c >= '\u0400' && c <= '\u04FF')))
			{
				return "sr";
			}

			return "en";
		}

		private async Task<ResponseSectionLabels?> FetchLabelsFromLlmAsync(string languageCode)
		{
			string prompt =
				"Return STRICT JSON only with localized section labels for the requested language.\n" +
				"Schema:\n" +
				"{\n" +
				"  \"short_answer_label\": \"...\",\n" +
				"  \"explanation_label\": \"...\",\n" +
				"  \"citations_label\": \"...\",\n" +
				"  \"confidence_label\": \"...\"\n" +
				"}\n\n" +
				$"Target language code: {languageCode}";

			string responseText = await RequestCompletionTextAsync("You produce localized UI labels.", prompt, 80).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(responseText))
			{
				return null;
			}

			string normalized = NormalizeCompletionText(responseText);
			using var doc = JsonDocument.Parse(normalized);
			var root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return null;
			}

			string shortAnswer = ReadJsonString(root, "short_answer_label");
			string explanation = ReadJsonString(root, "explanation_label");
			string citations = ReadJsonString(root, "citations_label");
			string confidence = ReadJsonString(root, "confidence_label");

			if (string.IsNullOrWhiteSpace(shortAnswer)
				|| string.IsNullOrWhiteSpace(explanation)
				|| string.IsNullOrWhiteSpace(citations)
				|| string.IsNullOrWhiteSpace(confidence))
			{
				return null;
			}

			return new ResponseSectionLabels(shortAnswer, explanation, citations, confidence);
		}

		private static string BuildFormattedAnswer(RegulationResponse response, ResponseSectionLabels labels)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"{labels.ShortAnswerLabel}: {response.ShortAnswer}");
			sb.AppendLine();
			sb.AppendLine($"{labels.ExplanationLabel}: {response.Explanation}");
			sb.AppendLine();
			sb.AppendLine($"{labels.CitationsLabel}:");
			if (response.Citations == null || response.Citations.Count == 0)
			{
				sb.AppendLine("   -");
			}
			else
			{
				foreach (var citation in response.Citations)
				{
					sb.AppendLine($"   - {citation.Citation}");
				}
			}
			sb.AppendLine();
			sb.AppendLine($"{labels.ConfidenceLabel}: {response.Confidence}");
			return sb.ToString().Trim();
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
					OpenAIApiKey = section.Parameters["APIKey"]?.Value,
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
