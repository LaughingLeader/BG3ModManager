using Avalonia.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class ControlExtensions
{
	public static void DesignSetup(this UserControl	control)
	{
		//Workaround for not being able to set the designer background to a darker color
		if (Design.IsDesignMode && (control.Background == null || control.Background == Brushes.Transparent))
		{
			control.Background = Brushes.Black;
		}
	}
}
