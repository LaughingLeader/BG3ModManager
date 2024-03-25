using System.Globalization;
using System.Windows.Data;

namespace ModManager.Converters;

public class StringToUriConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string str)
		{
			Uri result = null;
			if (Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out result))
			{
				return result;
			}
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Uri uri)
		{
			return uri.OriginalString;
		}
		return "";
	}
}
