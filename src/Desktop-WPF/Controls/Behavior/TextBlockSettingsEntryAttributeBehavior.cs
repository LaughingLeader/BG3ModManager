using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ModManager.Controls.Behavior;

public class TextBlockSettingsEntryAttributeBehavior
{
	public static readonly DependencyProperty PropertyProperty =
		DependencyProperty.RegisterAttached(
		"Property",
		typeof(string),
		typeof(TextBlockSettingsEntryAttributeBehavior),
		new UIPropertyMetadata("", OnPropertySet));

	public static readonly DependencyProperty TargetTypeProperty =
		DependencyProperty.RegisterAttached(
		"TargetType",
		typeof(Type),
		typeof(TextBlockSettingsEntryAttributeBehavior),
		new UIPropertyMetadata(null, OnTargetTypeSet));

	public static string GetProperty(DependencyObject element)
	{
		return (string)element.GetValue(PropertyProperty);
	}

	public static void SetProperty(DependencyObject element, string value)
	{
		element.SetValue(PropertyProperty, value);
	}

	public static Type GetTargetType(DependencyObject element)
	{
		return (Type)element.GetValue(TargetTypeProperty);
	}

	public static void SetTargetType(DependencyObject element, Type value)
	{
		element.SetValue(TargetTypeProperty, value);
	}

	private static void UpdateElement(TextBlock element, string propName = "", Type targetType = null)
	{
		if (targetType == null) targetType = GetTargetType(element);
		if (String.IsNullOrEmpty(propName)) propName = GetProperty(element);
		if (targetType != null && !String.IsNullOrEmpty(propName))
		{
			var prop = targetType.GetProperty(propName);
			var settingsEntry = prop.GetCustomAttribute<SettingsEntryAttribute>();
			if (settingsEntry != null)
			{
				element.Text = settingsEntry.DisplayName;
				element.ToolTip = settingsEntry.Tooltip;
			}
		}
	}

	static void OnPropertySet(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TextBlock element && e.NewValue is string propName)
		{
			UpdateElement(element, propName);
		}
	}

	static void OnTargetTypeSet(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if (sender is TextBlock element && e.NewValue is Type type)
		{
			UpdateElement(element, "", type);
		}
	}
}
