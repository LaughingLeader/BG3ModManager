using ModManager.Controls;

using System.Globalization;
using System.Windows.Data;

namespace ModManager.Converters;

public class StringToMarkdownConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string str && !String.IsNullOrEmpty(str) && parameter is Markdown markdown)
		{
			return markdown.Transform(str);
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
}
