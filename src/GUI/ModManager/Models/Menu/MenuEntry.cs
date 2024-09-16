using DynamicData.Binding;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModManager.Models.Menu;
public class MenuEntry(string? name = null, ICommand? command = null, string? tooltip = null) : ReactiveObject, IMenuEntry
{
	[Reactive] public string? DisplayName { get; } = name;
	[Reactive] public string? ToolTip { get; } = tooltip;
	public ICommand? Command { get; } = command;
	public ObservableCollectionExtended<IMenuEntry>? Children { get; set; }
}
