using Avalonia.Data.Converters;

using Material.Icons;

using ModManager.Models.Mod;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Converters;
public class ForceAllowInLoadOrderIconConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if(value is DivinityModData mod)
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
