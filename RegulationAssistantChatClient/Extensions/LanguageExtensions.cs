using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using ExternalServiceContracts.Common;
using RegulationAssistantChatClient.Properties;

namespace RegulationAssistantChatClient.Extensions
{
	/// <summary>
	/// Conversion helpers related to the <see cref="Language"/> enum used in query preferences.
	/// </summary>
	public static class LanguageExtensions
	{
		private static readonly Dictionary<string, Language> map = new(StringComparer.OrdinalIgnoreCase)
		{
			{"English", Language.En },
			{"En", Language.En },
			{"Serbian", Language.RS },
			{"Sr", Language.RS },
		};

		/// <summary>
		/// Cache of localized resource strings for language display names, keyed by culture name.
		/// Each entry maps localized display strings to the corresponding <see cref="Language"/> enum value.
		/// </summary>
		private static readonly ConcurrentDictionary<string, Dictionary<string, Language>> _resourceCache = new();

		/// <summary>
		/// Creates a <see cref="Language"/> enum value from a display string.
		/// </summary>
		/// <param name="languageName">Language as string to convert to enum. May be a localized display value.</param>
		/// <param name="culture">Optional culture to use when resolving localized resource strings. When null, <see cref="CultureInfo.CurrentUICulture"/> is used.</param>
		/// <returns>Mapped <see cref="Language"/> value. Defaults to <see cref="Language.RS"/> when not recognized.</returns>
		public static Language FromString(string? languageName, CultureInfo? culture = null)
		{
			if (string.IsNullOrWhiteSpace(languageName))
			{
				return Language.RS;
			}

			var key = languageName.Trim();

			if (LanguageExtensions.map.TryGetValue(key, out var lang))
			{
				return lang;
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

			if (Enum.TryParse<Language>(key, ignoreCase: true, out var parsed))
			{
				return parsed;
			}

			return Language.RS;
		}

		/// <summary>
		/// Builds a mapping of localized display strings to <see cref="Language"/> enum values for a specific culture by reading from the application's resource files.
		/// The method handles any exceptions that may occur during resource access and returns an empty dictionary if resources cannot be read.
		/// </summary>
		/// <param name="culture">Target culture used when reading resource values.</param>
		/// <returns>Mapping for the requested culture (case-insensitive keys).</returns>
		private static Dictionary<string, Language> BuildResourceMapForCulture(CultureInfo culture)
		{
			var dict = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);
			try
			{
				AddIfNotEmpty(dict, Resources.ResourceManager.GetString("LanguageOption_English", culture), Language.En);
				AddIfNotEmpty(dict, Resources.ResourceManager.GetString("LanguageOption_Serbian", culture), Language.RS);
			}
			catch
			{
				// Ignore resource errors and return what we have
			}
			return dict;
		}

		/// <summary>
		/// Adds an entry to <paramref name="dict"/> when <paramref name="value"/> is not null/empty. Trims the value and uses a case-insensitive key.
		/// </summary>
		private static void AddIfNotEmpty(Dictionary<string, Language> dict, string? value, Language lang)
		{
			if (!string.IsNullOrWhiteSpace(value) && !dict.ContainsKey(value.Trim()))
			{
				dict[value.Trim()] = lang;
			}
		}
	}
}