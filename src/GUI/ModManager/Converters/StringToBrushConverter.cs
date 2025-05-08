using Avalonia.Data.Converters;
using Avalonia.Media;

using ModManager.Styling;

using System.Globalization;

namespace ModManager.Converters;
public class StringToBrushConverter : IValueConverter
{
	public IBrush Default => ColorBrushCache.GetResourceBrush("SukiPrimaryColor");

	public virtual object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is string colorStr && colorStr.IsValid())
		{
			return ColorBrushCache.GetBrush(colorStr);
		}
		else if (parameter is string fallbackResource)
		{
			return ColorBrushCache.GetResourceBrush(fallbackResource);
		}

		//return ColorBrushCache.ThemeForegroundBrush;
		return Default;
	}

	public virtual object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}