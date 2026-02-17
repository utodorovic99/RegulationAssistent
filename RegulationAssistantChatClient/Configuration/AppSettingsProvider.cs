using System;
using System.IO;
using System.Text.Json;

namespace RegulationAssistantChatClient.Configuration
{
	/// <summary>
	/// Provider that lazily loads <see cref="AppSettings"/> from an optional JSON file named <c>appsettings.json</c>
	/// located in the application's base directory. When the file or expected properties are missing the provider
	/// returns a default <see cref="AppSettings"/> instance.
	/// </summary>
	public static class AppSettingsProvider
	{
		private static readonly Lazy<AppSettings> settings = new Lazy<AppSettings>(LoadFromFile);

		/// <summary>
		/// Gets the application settings instance. The value is loaded lazily on first access and cached for the lifetime of the process.
		/// </summary>
		public static AppSettings Settings
		{
			get
			{
				return settings.Value;
			}
		}

		/// <summary>
		/// Attempts to read <c>appsettings.json</c> from the application's base directory and extract
		/// the <c>RegulationQueryService:BaseUrl</c> value. The method tolerates missing files and
		/// malformed content and will return a default <see cref="AppSettings"/> when parsing fails.
		/// </summary>
		/// <returns>An <see cref="AppSettings"/> instance containing values obtained from the file or defaults.</returns>
		private static AppSettings LoadFromFile()
		{
			try
			{
				string basePath = AppContext.BaseDirectory ?? Environment.CurrentDirectory;
				string configPath = Path.Combine(basePath, "appsettings.json");
				if (!File.Exists(configPath))
				{
					return new AppSettings();
				}

				string json = File.ReadAllText(configPath);
				using JsonDocument doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("RegulationQueryService", out JsonElement section))
				{
					if (section.TryGetProperty("BaseUrl", out JsonElement urlElem) && urlElem.ValueKind == JsonValueKind.String)
					{
						string? url = urlElem.GetString();
						if (!string.IsNullOrWhiteSpace(url))
						{
							return new AppSettings { RegulationQueryServiceBaseUrl = url };
						}
					}
				}
			}
			catch
			{
			}

			return new AppSettings();
		}
	}
}