using System;
using System.Globalization;
using System.Windows.Data;

namespace RegulationAssistantChatClient.Converters
{
	/// <summary>
	/// Value converter that multiplies a numeric input (double) by a provided factor.
	/// Useful in XAML bindings when an element size needs to be a proportion of another size
	/// (for example, setting a max width to70% of the available width).
	/// </summary>
	public class MultiplyConverter : IValueConverter
	{
		/// <summary>
		/// Converts the provided <paramref name="value"/> (expected to be a <see cref="double"/>)
		/// by multiplying it with the conversion factor supplied in <paramref name="parameter"/>.
		/// The parameter may be a string parseable as a double (using invariant culture) or a double value.
		/// If the input is not a double the original value is returned unchanged.
		/// </summary>
		/// <param name="value">The value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property. Not used by this converter.</param>
		/// <param name="parameter">Conversion factor. May be a string or double. When null the factor defaults to1.0.</param>
		/// <param name="culture">The culture to use in the converter. Not used for parsing numeric parameter (invariant culture is used instead).</param>
		/// <returns>The multiplied double when the input is a double; otherwise returns the original <paramref name="value"/>.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is double d)
			{
				double factor =1.0;
				if (parameter != null)
				{
					if (parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p))
					{
						factor = p;
					}
					else if (parameter is double pd)
					{
						factor = pd;
					}
				}

				return d * factor;
			}

			return value;
		}

		/// <summary>
		/// ConvertBack is not supported by this converter because the multiplication operation
		/// cannot be safely inverted without additional context. Calling this method will throw <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="value">The value produced by the binding target.</param>
		/// <param name="targetType">The type to convert to. Not used.</param>
		/// <param name="parameter">Optional parameter. Not used.</param>
		/// <param name="culture">The culture to use in the converter. Not used.</param>
		/// <returns>Never returns; always throws.</returns>
		/// <exception cref="NotSupportedException">Thrown always because reverse conversion is not implemented.</exception>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}