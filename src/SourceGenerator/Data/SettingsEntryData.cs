namespace ModManager.Data;
public readonly record struct SettingsEntryData
{
	public readonly string PropertyName;
	public readonly string PropertyType;
	public readonly string? DisplayName;
	public readonly string? ToolTip;
	public readonly bool IsDebug;
	public readonly bool HideFromUI;

	public SettingsEntryData(string propertyName, string propertyType, string? name, string? tooltip, bool isDebug = false, bool hide = false)
	{
		PropertyName = propertyName;
		PropertyType = propertyType;
		DisplayName = name;
		ToolTip = tooltip;
		IsDebug = isDebug;
		HideFromUI = hide;
	}

	public static SettingsEntryData FromAttribute(IPropertySymbol symbol, AttributeData attribute)
	{
		var propertyName = symbol.Name;
		var propertyType = symbol.Type.Name;
		var name = "";
		var tooltip = "";
		var isDebug = false;
		var hideFromUI = false;

		foreach(var namedArg in attribute.NamedArguments)
		{
			var value = namedArg.Value.Value?.ToString();
			switch (namedArg.Key)
			{
				case "DisplayName":
					name = value;
					break;
				case "ToolTip":
					tooltip = value;
					break;
				case "IsDebug":
					isDebug = bool.Parse(value);
					break;
				case "HideFromUI":
					hideFromUI = bool.Parse(value);
					break;
			}
		}

		var i = 0;
		foreach(var arg in attribute.ConstructorArguments)
		{
			switch (i)
			{
				case 0:
					name = arg.Value?.ToString();
					break;
				case 1:
					tooltip = arg.Value?.ToString();
					break;
				case 2:
					isDebug = bool.Parse(arg.Value?.ToString());
					break;
				case 3:
					hideFromUI = bool.Parse(arg.Value?.ToString());
					break;
			}
			i++;
		}

		return new SettingsEntryData(propertyName, propertyType, name, tooltip, isDebug, hideFromUI);
	}
}