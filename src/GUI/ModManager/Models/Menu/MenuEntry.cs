using DynamicData.Binding;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

	public override string ToString() => DisplayName ?? "";
}

/*public class MenuEntry(string? name = null, ICommand? command = null, string? tooltip = null) : ReactiveObject, IMenuEntry
{
	public string? DisplayName { get; } = name;
	public string? ToolTip { get; } = tooltip;
	public ICommand? Command { get; } = command;
	public ObservableCollectionExtended<IMenuEntry>? Children { get; set; }
}
*/