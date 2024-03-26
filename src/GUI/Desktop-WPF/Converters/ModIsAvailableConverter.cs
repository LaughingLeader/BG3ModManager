using ModManager.Models.Mod;

using System.Globalization;
using System.Windows.Data;

namespace ModManager.Converters;

public class ModIsAvailableConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is IDivinityModData data)
		{
			return AppServices.Mods.ModIsAvailable(data);
		}

		return false;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
