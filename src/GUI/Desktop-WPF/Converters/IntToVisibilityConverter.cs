using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModManager.Converters;

public class IntToVisibilityConverter : IValueConverter
{
	public static Visibility FromInt(int v) => v > 0 ? Visibility.Visible : Visibility.Collapsed;

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is int intVal)
		{
			return intVal == 0 ? Visibility.Collapsed : Visibility.Visible;
		}
		return Visibility.Visible;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
