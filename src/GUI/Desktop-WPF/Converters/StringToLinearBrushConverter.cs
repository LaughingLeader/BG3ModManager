using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModManager.Converters;

public class StringToLinearBrushConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string str && !String.IsNullOrEmpty(str))
		{
			var startColor = (Color)ColorConverter.ConvertFromString(str);
			var endColor = Color.FromArgb(startColor.A, (byte)(startColor.R * 0.7), (byte)(startColor.G * 0.7), (byte)(startColor.B * 0.7));
			return new LinearGradientBrush(startColor, endColor, 90.0d);
		}
		return null;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
