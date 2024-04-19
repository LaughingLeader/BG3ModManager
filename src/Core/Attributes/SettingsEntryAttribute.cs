using DynamicData.Binding;

using System.Reflection;

namespace ModManager;

public class SettingsEntryAttribute : Attribute
{
	public string? DisplayName { get; set; }
	public string? ToolTip { get; set; }
	public string? BindTo { get; set; }
	public string? BindVisibilityTo { get; set; }
	public bool DisableAutoGen { get; set; }

	public SettingsEntryAttribute(string displayName = "", string tooltip = "", string? bindTo = null, string? bindVisibilityTo = null, bool disableAutoGen = false)
	{
		DisplayName = displayName;
		ToolTip = tooltip;
		BindTo = bindTo;
		BindVisibilityTo = bindVisibilityTo;
		DisableAutoGen = disableAutoGen;
	}
}

public static class SettingsEntryAttributeExtensions
{
	public static List<SettingsAttributeProperty> GetSettingsAttributes(this ReactiveObject model)
	{
		var props = model.GetType().GetProperties()
			.Select(x => SettingsAttributeProperty.FromProperty(x))
			.Where(x => x.Attribute != null).ToList();
		return props;
	}

	public static IObservable<ReactiveObject> WhenAnySettingsChange(this ReactiveObject model)
	{
		var props = model.GetType().GetProperties()
			.Select(x => SettingsAttributeProperty.FromProperty(x))
			.Where(x => x.Attribute != null).Select(x => x.Property.Name).ToArray();
		return model.WhenAnyPropertyChanged(props);
	}
}

public struct SettingsAttributeProperty
{
	public PropertyInfo Property { get; set; }
	public SettingsEntryAttribute Attribute { get; set; }

	public static SettingsAttributeProperty FromProperty(PropertyInfo property)
	{
		return new SettingsAttributeProperty
		{
			Property = property,
			Attribute = property.GetCustomAttribute<SettingsEntryAttribute>()
		};
	}
}
