using Avalonia.Data.Converters;
using Avalonia.Media;

using ModManager.Styling;

using System.Globalization;

namespace ModManager.Converters;
public class ModBackgroundColorConverter : StringToBrushConverter
{
	public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var result = base.Convert(value, targetType, parameter, culture);
		if(result != Default)
		{
			return result;
		}
		return Brushes.Transparent;
	}
}