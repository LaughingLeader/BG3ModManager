using DynamicData;
using DynamicData.Binding;

using System.Windows.Input;

namespace ModManager.Models.View;
public abstract class TreeViewEntry : ReactiveObject
{
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsSelected { get; set; }

	private readonly SourceList<TreeViewEntry> _children;

	private readonly ReadOnlyObservableCollection<TreeViewEntry> _uiChildren;
	public ReadOnlyObservableCollection<TreeViewEntry> Children => _uiChildren;

	public void AddChild(TreeViewEntry child) => _children.Add(child);
	public void AddChild(IEnumerable<TreeViewEntry> children) => _children.AddRange(children);

	public abstract object ViewModel { get; }

	public ICommand ToggleCommand { get; }

	public TreeViewEntry()
	{
		_children = new();
		_children.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out _uiChildren).DisposeMany().Subscribe();

		ToggleCommand = ReactiveCommand.Create(() => IsExpanded = !IsExpanded);
	}
}
