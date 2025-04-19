using DynamicData.Binding;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModManager.Models.Menu;
public class MenuEntry : ReactiveObject, IMenuEntry
{
	[Reactive] public string? DisplayName { get; set; }
	[Reactive] public string? ToolTip { get; set; }
	[Reactive] public ICommand? Command { get; set; }
	public ObservableCollectionExtended<IMenuEntry>? Children { get; set; }

	public MenuEntry() { }

	public MenuEntry(string? name = null, ICommand? command = null, string? tooltip = null)
	{
		DisplayName = name;
		Command = command;
		ToolTip = tooltip;
	}

	public static MenuEntry FromKeybinding(ICommand command, string propertyName,
		Dictionary<string,KeybindingAttribute?> properties,
		ObservableCollectionExtended<IMenuEntry>? children = null)
	{
		if(properties.TryGetValue(propertyName, out var keybinding) && keybinding != null)
		{
			var entry = new MenuEntry()
			{
				DisplayName = keybinding.DisplayName,
				ToolTip = keybinding.ToolTip,
				Command = command
			};
			if(children != null)
			{
				entry.Children = children;
			}
			return entry;
		}
		throw new ArgumentException($"Property '{propertyName}' is invalid or is missing a KeybindingAttribute.", nameof(propertyName));
	}

	public override string ToString() => DisplayName ?? "";
}