using System.Windows.Data;

namespace ModManager.Converters;

class DivinityGameLaunchWindowActionToStringConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return value.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return (DivinityGameLaunchWindowAction)Enum.Parse(typeof(DivinityGameLaunchWindowAction), value.ToString(), true);
	}
}
