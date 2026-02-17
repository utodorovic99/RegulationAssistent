using ExternalServiceContracts.Common;
using RegulationAssistantChatClient.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace RegulationAssistantChatClient.Extensions
{
	/// <summary>
	/// Helper conversions for <see cref="AnswerStyle"/>. Centralizes mapping between
	/// user-facing labels (for example "Concise with Citations") and the
	/// <see cref="ExternalServiceContracts.Common.AnswerStyle"/> enum values.
	/// </summary>
	public static class AnswerStyleExtensions
	{
		private static readonly Dictionary<string, AnswerStyle> map = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "Concise with Citations", AnswerStyle.ConciseWithCitations },
			{ "Detailed", AnswerStyle.Detailed },
			{ "Bullet Points", AnswerStyle.BulletPoints },
		};

		private static readonly ConcurrentDictionary<string, Dictionary<string, AnswerStyle>> _resourceCache = new ConcurrentDictionary<string, Dictionary<string, AnswerStyle>>();

		/// <summary>
		/// Converts a display string into the corresponding <see cref="AnswerStyle"/> enum.
		/// Returns <see cref="AnswerStyle.Other"/> when the input is null, empty or not recognized.
		/// The method first checks invariant short forms, then localized resource strings for the specified culture,
		/// then invariant resources, and finally attempts an enum parse before falling back to <see cref="AnswerStyle.Other"/>.
		/// </summary>
		/// <param name="value">Display string (e.g. "Concise with Citations", "Detailed", "Bullet Points").</param>
		/// <param name="culture">Optional culture to use when resolving localized resource strings. When null, <see cref="CultureInfo.CurrentUICulture"/> is used.</param>
		/// <returns>Mapped <see cref="AnswerStyle"/> value.</returns>
		public static AnswerStyle FromString(string? value, CultureInfo? culture = null)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return AnswerStyle.Other;
			}

			var key = value.Trim();

			if (AnswerStyleExtensions.map.TryGetValue(key, out var result))
			{
				return result;
			}

			culture ??= CultureInfo.CurrentUICulture ?? CultureInfo.InvariantCulture;
			var cultureKey = string.IsNullOrEmpty(culture.Name) ? "_invariant_" : culture.Name;

			var map = _resourceCache.GetOrAdd(cultureKey, _ => BuildResourceMapForCulture(culture));
			if (map.TryGetValue(key, out var mapped))
			{
				return mapped;
			}

			if (cultureKey != "_invariant_")
			{
				var invariant = _resourceCache.GetOrAdd("_invariant_", _ => BuildResourceMapForCulture(CultureInfo.InvariantCulture));
				if (invariant.TryGetValue(key, out mapped))
				{
					return mapped;
				}
			}

			if (Enum.TryParse<AnswerStyle>(key.Replace(" ", ""), ignoreCase: true, out var parsed))
			{
				return parsed;
			}

			return AnswerStyle.Other;
		}

		/// <summary>
		/// Builds a mapping of localized display strings to <see cref="AnswerStyle"/> enum values for a specific culture by reading from the application's resource files.
		/// The method handles any exceptions that may occur during resource access and returns an empty dictionary if resources cannot be read.
		/// </summary>
		/// <param name="culture">Target culture used when reading resource values.</param>
		/// <returns>Mapping for the requested culture (case-insensitive keys).</returns>
		private static Dictionary<string, AnswerStyle> BuildResourceMapForCulture(CultureInfo culture)
		{
			var dict = new Dictionary<string, AnswerStyle>(StringComparer.OrdinalIgnoreCase);
			try
			{
				AddIfNotEmpty(dict, Resources.ResourceManager.GetString("AnswerStyle_Concise", culture), AnswerStyle.ConciseWithCitations);
				AddIfNotEmpty(dict, Resources.ResourceManager.GetString("AnswerStyle_Detailed", culture), AnswerStyle.Detailed);
				AddIfNotEmpty(dict, Resources.ResourceManager.GetString("AnswerStyle_BulletPoints", culture), AnswerStyle.BulletPoints);
			}
			catch
			{
			}

			return dict;
		}

		/// <summary>
		/// Adds an entry to <paramref name="dict"/> when <paramref name="value"/> is not null/empty. Trims the value and uses a case-insensitive key.
		/// </summary>
		private static void AddIfNotEmpty(Dictionary<string, AnswerStyle> dict, string? value, AnswerStyle style)
		{
			if (!string.IsNullOrWhiteSpace(value) && !dict.ContainsKey(value.Trim()))
			{
				dict[value.Trim()] = style;
			}
		}
	}
}
