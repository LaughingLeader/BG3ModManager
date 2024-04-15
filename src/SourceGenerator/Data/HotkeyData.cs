using Microsoft.CodeAnalysis.CSharp;

using System;
using System.Collections.Generic;
using System.Text;

namespace ModManager.SourceGenerator.Data;

public readonly record struct HotkeyData
{
	public readonly string Id;
	public readonly string PropertyName;
	public readonly string PropertyType;
	public readonly string? Key;
	public readonly string? Modifiers;
	public readonly string? DisplayName;
	public readonly string? ToolTip;
	public readonly string? MenuCategory;

	public HotkeyData(string id, string propertyName, string propertyType, string? name, string? key, string? modifiers = null, string? tooltip = null, string? category = null)
	{
		Id = id;
		PropertyName = propertyName;
		PropertyType = propertyType;
		Key = key;
		Modifiers = modifiers;
		DisplayName = name;
		ToolTip = tooltip;
		MenuCategory = category;
	}

	private static string Escape(string str) => "\"" + str + "\"";

	public override string ToString()
	{
		var args = new List<string>
			{
				"\"" + Id + "\"",
				DisplayName ?? "\"\"",
				PropertyName,
			};
		if (Key != null) args.Add(Key);
		if (Modifiers != null) args.Add(Modifiers);
		return $"keys.RegisterCommand({string.Join(", ", args)})";
	}

	public static HotkeyData FromAttribute(IPropertySymbol symbol, AttributeData attribute)
	{
		var propertyName = symbol.Name;
		var propertyType = symbol.Type.Name;
		string? name = null;
		string? tooltip = null;
		string? key = null;
		string? modifiers = null;
		string? category = null;
		var id = $"{symbol.ContainingType.Name}_{propertyName}";

		foreach (var namedArg in attribute.NamedArguments)
		{
			var value = namedArg.Value.ToCSharpString();
			switch (namedArg.Key)
			{
				case "DisplayName":
					name = value;
					break;
				case "ToolTip":
					tooltip = value;
					break;
				case "MenuCategory":
					category = value;
					break;
				case "Key":
					if (value?.EndsWith("None") != true) key = value;
					break;
				case "Modifiers":
					if(value?.EndsWith("None") != true) modifiers = value;
					break;
			}
		}

		//(string displayName, Key key, ModifierKeys modifiers = KeyModifiers.None, string? tooltip = null, string? menuCategory = null)
		var i = 0;
		foreach (var arg in attribute.ConstructorArguments)
		{
			var value = arg.ToCSharpString();
			switch (i)
			{
				case 0:
					name = value;
					break;
				case 1:
					if (value?.EndsWith("None") != true) key = value;
					break;
				case 2:
					if (value?.EndsWith("None") != true) modifiers = value;
					break;
				case 3:
					tooltip = value;
					break;
				case 4:
					category = value;
					break;
			}
			i++;
		}

		return new HotkeyData(id, propertyName, propertyType, name, key, modifiers, tooltip, category);
	}
}