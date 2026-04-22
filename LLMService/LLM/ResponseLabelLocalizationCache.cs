using System.Collections.Concurrent;

namespace ResponseService
{
	internal sealed class ResponseLabelLocalizationCache
	{
		private static readonly ConcurrentDictionary<string, ResponseSectionLabels> cache = new ConcurrentDictionary<string, ResponseSectionLabels>(StringComparer.OrdinalIgnoreCase)
		{
			["sr"] = new ResponseSectionLabels("Kratak odgovor", "Objasnjenje", "Citati", "Tacnost"),
			["en"] = new ResponseSectionLabels("Short Answer", "Explanation", "Citations", "Confidence"),
			["de"] = new ResponseSectionLabels("Kurze Antwort", "Erklärung", "Zitate", "Genauigkeit"),
		};

		public bool TryGet(string? languageCode, out ResponseSectionLabels labels)
		{
			labels = default!;
			var normalized = Normalize(languageCode);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return false;
			}

			return cache.TryGetValue(normalized, out labels);
		}

		public void Set(string? languageCode, ResponseSectionLabels labels)
		{
			var normalized = Normalize(languageCode);
			if (string.IsNullOrWhiteSpace(normalized))
			{
				return;
			}

			cache[normalized] = labels;
		}

		public ResponseSectionLabels GetEnglishFallback() => cache["en"];

		private static string Normalize(string? languageCode)
		{
			if (string.IsNullOrWhiteSpace(languageCode))
			{
				return string.Empty;
			}

			var value = languageCode.Trim().ToLowerInvariant();
			if (value.Length > 2)
			{
				value = value[..2];
			}

			return value;
		}
	}

	internal readonly record struct ResponseSectionLabels(
		string ShortAnswerLabel,
		string ExplanationLabel,
		string CitationsLabel,
		string ConfidenceLabel);
}
