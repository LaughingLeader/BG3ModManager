using Avalonia.Data.Converters;

using Material.Icons;

using ModManager.Models.Mod;

using System.Globalization;

namespace ModManager.Converters;
public class ForceAllowInLoadOrderIconConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if(value is ModData mod)
		{
			if(mod.ForceAllowInLoadOrder)
			{
				return MaterialIconKind.ArrowExpandDown;
			}
			else
			{
				return MaterialIconKind.ArrowExpandUp;
			}
		}
		return MaterialIconKind.Warning;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}
