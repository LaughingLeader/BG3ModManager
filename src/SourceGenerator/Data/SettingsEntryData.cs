namespace ModManager.SourceGenerator.Data;
public readonly record struct SettingsEntryData
{
	public readonly string PropertyName;
	public readonly ITypeSymbol PropertyType;
	public readonly string PropertyTypeName;
	public readonly string? DisplayName;
	public readonly string? ToolTip;
	public readonly string? BindTo;
	public readonly string? BindVisibilityTo;
	public readonly bool DisableAutoGen;

	public SettingsEntryData(string propertyName, ITypeSymbol propertyType, string? name, string? tooltip, string? bindTo, string? bindVisibilityTo, bool disableAutoGen)
	{
		PropertyName = propertyName;
		PropertyType = propertyType;
		PropertyTypeName = propertyType.Name;
		DisplayName = name;
		ToolTip = tooltip;
		BindTo = bindTo;
		BindVisibilityTo = bindVisibilityTo;
		DisableAutoGen = disableAutoGen;
	}

	public static SettingsEntryData FromAttribute(IPropertySymbol symbol, AttributeData attribute)
	{
		var propertyName = symbol.Name;
		var name = "";
		var tooltip = "";
		string? bindTo = null;
		string? bindVisibilityTo = null;
		bool disableAutoGen = false;

		for (var i = 0; i < attribute.ConstructorArguments.Length; i++)
		{
			var arg = attribute.ConstructorArguments[i];
			if (arg.IsNull) continue;

			var value = arg.Value?.ToString();
			switch (i)
			{
				case 0:
					name = value;
					break;
				case 1:
					tooltip = value;
					break;
				case 2:
					bindTo = value;
					break;
				case 3:
					bindVisibilityTo = value;
					break;
				case 4:
					disableAutoGen = bool.Parse(value);
					break;
			}
		}

		foreach (var namedArg in attribute.NamedArguments)
		{
			if (namedArg.Value.IsNull) continue;

			var value = namedArg.Value.Value?.ToString();
			switch (namedArg.Key)
			{
				case "DisplayName":
					name = value;
					break;
				case "ToolTip":
					tooltip = value;
					break;
				case "BindTo":
					bindTo = value;
					break;
				case "BindVisibilityTo":
					bindVisibilityTo = value;
					break;
				case "DisableAutoGen":
					disableAutoGen = bool.Parse(value);
					break;
			}
		}

		if(propertyName == "DebugModeEnabled")
		{
			System.Diagnostics.Trace.WriteLine(propertyName);
		}

		return new SettingsEntryData(propertyName, symbol.Type, name, tooltip, bindTo, bindVisibilityTo, disableAutoGen);
	}
}