using Humanizer;

using System.Windows.Data;

namespace ModManager.Converters;

public class TimeSpanToReadableStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if (value is TimeSpan time)
		{
			if (time <= TimeSpan.Zero)
			{
				return "0 minutes (Disabled)";
			}
			return time.Humanize(2);
		}
		return String.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return TimeSpan.Zero;
	}
}
