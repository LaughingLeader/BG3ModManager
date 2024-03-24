using System.Windows;

namespace ModManager.Controls.Behavior;

public class ScreenReaderHelperBehavior
{
	public static string GetName(DependencyObject element)
	{
		return (string)element.GetValue(NameProperty);
	}

	public static void SetName(DependencyObject element, string value)
	{
		element.SetValue(NameProperty, value);
	}

	public static readonly DependencyProperty NameProperty =
		DependencyProperty.RegisterAttached(
		"Name",
		typeof(string),
		typeof(ScreenReaderHelperBehavior),
		new UIPropertyMetadata("", OnName));

	static void OnName(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
	{
		System.Windows.Automation.AutomationProperties.SetName(depObj, (string)e.NewValue);
	}

	public static string GetHelpText(DependencyObject element)
	{
		return (string)element.GetValue(HelpTextProperty);
	}

	public static void SetHelpText(DependencyObject element, string value)
	{
		element.SetValue(HelpTextProperty, value);
	}

	public static readonly DependencyProperty HelpTextProperty =
		DependencyProperty.RegisterAttached(
		"HelpText",
		typeof(string),
		typeof(ScreenReaderHelperBehavior),
		new UIPropertyMetadata("", OnHelpText));

	static void OnHelpText(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
	{
		System.Windows.Automation.AutomationProperties.SetHelpText(depObj, (string)e.NewValue);
	}

	public static bool GetAutomatic(DependencyObject element)
	{
		return (bool)element.GetValue(AutomaticProperty);
	}

	public static void SetAutomatic(DependencyObject element, bool value)
	{
		element.SetValue(AutomaticProperty, value);
	}

	public static readonly DependencyProperty AutomaticProperty =
		DependencyProperty.RegisterAttached(
		"Automatic",
		typeof(bool),
		typeof(ScreenReaderHelperBehavior),
		new UIPropertyMetadata(false, OnAutomaticChanged));

	static void OnAutomaticChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
	{
		if (depObj is FrameworkElement element)
		{
			if (e.NewValue is bool enabled)
			{
				if (enabled)
				{
					element.DataContextChanged += Element_DataContextChanged;
					if (element.DataContext != null)
					{
						Element_DataContextChanged(element, new DependencyPropertyChangedEventArgs(FrameworkElement.DataContextProperty, null, element.DataContext));
					}
				}
				else
				{
					element.DataContextChanged -= Element_DataContextChanged;
				}
			}
		}
	}

	private static void Element_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is null)
		{
			return;
		}
		var depObj = sender as DependencyObject;
		var t = e.NewValue.GetType();
		var attributes = t.GetCustomAttributes(typeof(ScreenReaderHelperAttribute), true);
		if (attributes.Length > 0)
		{
			foreach (var attr in attributes)
			{
				var sr = (ScreenReaderHelperAttribute)attr;
				if (!String.IsNullOrEmpty(sr.Name))
				{
					var prop = t.GetProperty(sr.Name);
					if (prop != null)
					{
						var propValue = (string)prop.GetValue(e.NewValue);
						if (!String.IsNullOrEmpty(propValue))
						{
							System.Windows.Automation.AutomationProperties.SetName(depObj, propValue);
							//Trace.WriteLine($"Set AutomationProperties.Name to {propValue}");
						}
					}
				}
				if (!String.IsNullOrEmpty(sr.HelpText))
				{
					var prop = t.GetProperty(sr.HelpText);
					if (prop != null)
					{
						var propValue = (string)prop.GetValue(e.NewValue);
						if (!String.IsNullOrEmpty(propValue))
						{
							System.Windows.Automation.AutomationProperties.SetHelpText(depObj, propValue);
							//Trace.WriteLine($"Set AutomationProperties.HelpText to {propValue}");
						}
					}
				}
			}
		}
	}
}
