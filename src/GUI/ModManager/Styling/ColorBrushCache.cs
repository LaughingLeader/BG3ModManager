using Avalonia.Media;

namespace ModManager.Styling;
internal static class ColorBrushCache
{
	private static readonly Dictionary<string, IBrush> _colorBrushCache;

	static ColorBrushCache()
	{
		_colorBrushCache = [];
	}

	//private var 

	public static IBrush GetBrush(string value)
	{
		if (_colorBrushCache.TryGetValue(value, out var brush))
		{
			return brush;
		}
		try
		{
			if (Brush.Parse(value) is IBrush newBrush)
			{
				_colorBrushCache.Add(value, newBrush);
				return newBrush;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error converting brush: {ex}");
		}
		return Brushes.Transparent;
	}

	public static IBrush GetResourceBrush(string value)
	{
		if (_colorBrushCache.TryGetValue(value, out var brush))
		{
			return brush;
		}
		try
		{
			if (App.Current.TryGetResource(value, App.Current.RequestedThemeVariant, out var resourceObj))
			{
				if(resourceObj is IBrush resourceBrush)
				{
					//_colorBrushCache.Add(value, resourceBrush);
					return resourceBrush;
				}
				if (resourceObj is Color resourceColor)
				{
					var newBrush = new SolidColorBrush(resourceColor);
					_colorBrushCache.Add(value, newBrush);
					return newBrush;
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error converting brush: {ex}");
		}
		return Brushes.Transparent;
	}

	public static IBrush ThemeForegroundBrush => GetResourceBrush("ThemeForegroundBrush");
}
