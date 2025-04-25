using Avalonia.Data.Converters;
using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Converters.Mod;
public class ModBackgroundColorConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		//DivinityApp.Log($"[ModBackgroundColorConverter] value({value}) parameter({parameter})");
		if (value is string colorStr && colorStr.IsValid())
		{
			return ColorBrushCache.GetBrush(colorStr);
		}
		else if (parameter is string fallbackResource)
		{
			return ColorBrushCache.GetResourceBrush(fallbackResource);
		}
		
		return Brushes.Transparent;
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}