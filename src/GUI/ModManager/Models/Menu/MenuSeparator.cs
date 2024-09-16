using DynamicData.Binding;

using System.Windows.Input;

namespace ModManager.Models.Menu;
public class MenuSeparator : IMenuEntry
{
	public string? DisplayName { get; }
	public string? ToolTip { get; }
	public ICommand? Command { get; }
	public ObservableCollectionExtended<IMenuEntry>? Children { get; }
}
