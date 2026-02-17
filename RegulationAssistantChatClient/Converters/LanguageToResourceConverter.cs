using System;
using System.Globalization;
using System.Windows.Data;

namespace RegulationAssistantChatClient.Converters
{
	/// <summary>
	/// Converts a language identifier (for example "English" or "Serbian") and a resource key
	/// (provided via the <see cref="IValueConverter.Convert"/>'s parameter) into a localized string
	/// loaded from the application's resources.
	/// </summary>
	public class LanguageToResourceConverter : IValueConverter
	{
		/// <summary>
		/// Converts a language and resource key into the localized string.
		/// The converter expects the incoming <paramref name="value"/> to be a language string
		/// and the <paramref name="parameter"/> to be the resource key (e.g. "SendButton").
		/// </summary>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string language = value as string;
			if (language is null)
			{
				language = string.Empty;
			}

			string key = parameter as string;
			if (key is null)
			{
				key = string.Empty;
			}

			if (string.IsNullOrEmpty(key)) return string.Empty;

			try
			{
				CultureInfo ci = CultureInfo.InvariantCulture;
				if (string.Equals(language, "Serbian", StringComparison.OrdinalIgnoreCase))
				{
					ci = new CultureInfo("sr");
				}
				else if (string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
				{
					ci = CultureInfo.InvariantCulture;
				}

				var rm = RegulationAssistantChatClient.Properties.Resources.ResourceManager;
				string? val = rm.GetString(key, ci);
				if (string.IsNullOrWhiteSpace(val))
				{
					val = rm.GetString(key, CultureInfo.InvariantCulture);
				}

				if (string.IsNullOrWhiteSpace(val))
				{
					return key;
				}

				return val;
			}
			catch
			{
				return key;
			}
		}

		/// <summary>
		/// ConvertBack is not supported by this converter and will throw <see cref="NotSupportedException"/>.
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}