using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ModManager.Converters;

public class StringToSolidBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string str && !String.IsNullOrEmpty(str))
		{
			var color = (Color)ColorConverter.ConvertFromString(str);
			return new SolidColorBrush(color);
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
