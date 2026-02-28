using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RegulationAssistantChatClient.Converters
{
	/// <summary>
	/// Converts a boolean value to one of two <see cref="Brush"/> instances.
	/// Used in XAML bindings to present different brushes for true/false states.
	/// </summary>
	public class BoolToBrushConverter : IValueConverter
	{
		/// <summary>
		/// Gets or sets brush returned when the input value is true.
		/// </summary>
		public Brush TrueBrush { get; set; } = Brushes.DodgerBlue;

		/// <summary>
		/// Gets or sets brush returned when the input value is false.
		/// </summary>
		public Brush FalseBrush { get; set; } = Brushes.Gray;

		/// <summary>
		/// Converts a boolean input to the corresponding <see cref="Brush"/>.
		/// If the value is not a boolean, returns <see cref="FalseBrush"/>.
		/// </summary>
		/// <param name="value">The source value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property.</param>
		/// <param name="parameter">Optional converter parameter.</param>
		/// <param name="culture">The culture to use in the converter.</param>
		/// <returns>A <see cref="Brush"/> instance chosen based on the boolean input.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool flag = false;
			if (value is bool b)
			{
				flag = b;
			}

			return flag ? TrueBrush : FalseBrush;
		}

		/// <summary>
		/// ConvertBack is not supported for this converter and will throw <see cref="NotSupportedException"/>.
		/// </summary>
		/// <param name="value">The value that is produced by the binding target.</param>
		/// <param name="targetType">The type to convert to.</param>
		/// <param name="parameter">Optional converter parameter.</param>
		/// <param name="culture">The culture to use in the converter.</param>
		/// <returns>Never returns; always throws.</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}