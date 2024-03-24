﻿using ModManager.Models.Mod;
using ModManager.Windows;

using System.Globalization;
using System.Windows.Data;

namespace ModManager.Converters;

public class ModIsAvailableConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is IDivinityModData data)
		{
			return Services.Mods.ModIsAvailable(data);
		}

		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
