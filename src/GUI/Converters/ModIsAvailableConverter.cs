﻿using DivinityModManager.Models;
using DivinityModManager.Windows;

using System.Globalization;
using System.Windows.Data;

namespace DivinityModManager.Converters;

public class ModIsAvailableConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is IDivinityModData data)
		{
			return MainWindow.Self?.ViewModel.ModIsAvailable(data);
		}

		return false;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return null;
	}
}
