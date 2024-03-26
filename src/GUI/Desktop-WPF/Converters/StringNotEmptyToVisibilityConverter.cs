using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ModManager.Converters;

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var reverse = false;
		if (parameter != null)
		{
			if (parameter is int reverseInt)
			{
				reverse = reverseInt > 0;
			}
			else if (parameter is bool r)
			{
				reverse = r;
			}
		}

		if (value is string v)
		{
			if (!reverse)
			{
				return !String.IsNullOrWhiteSpace(v) ? Visibility.Visible : Visibility.Collapsed;
			}
			else
			{
				return String.IsNullOrWhiteSpace(v) ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		return Visibility.Visible;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return null;
	}
}
