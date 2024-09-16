using DynamicData.Binding;

using System.Windows.Input;

namespace ModManager.Models.Menu;
public interface IMenuEntry
{
	public string? DisplayName { get; }
	public string? ToolTip { get; }
	public ICommand? Command { get; }
	public ObservableCollectionExtended<IMenuEntry>? Children { get; }
}
