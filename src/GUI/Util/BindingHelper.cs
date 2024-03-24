using System.Windows.Controls;
using System.Windows.Data;

namespace ModManager.Util;

public static class BindingHelper
{
	public static void CreateCommandBinding(Button button, string vmProperty, object source)
	{
		Binding binding = new(vmProperty)
		{
			Source = source,
			Mode = BindingMode.OneWay
		};
		button.SetBinding(Button.CommandProperty, binding);
	}
	public static void CreateCommandBinding(MenuItem button, string vmProperty, object source)
	{
		Binding binding = new(vmProperty)
		{
			Source = source,
			Mode = BindingMode.OneWay
		};
		button.SetBinding(MenuItem.CommandProperty, binding);
	}
}
