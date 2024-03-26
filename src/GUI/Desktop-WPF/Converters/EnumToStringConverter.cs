using System.Windows.Data;

namespace ModManager.Converters;

class DivinityGameLaunchWindowActionToStringConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		return value?.ToString();
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
	{
		if(Enum.TryParse<DivinityGameLaunchWindowAction>(value?.ToString(), true, out var action))
		{
			return action;
		}
		return DivinityGameLaunchWindowAction.None;
	}
}
