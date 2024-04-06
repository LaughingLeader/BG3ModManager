using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using DynamicData;

using ModManager.Models.Mod;
using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;
public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
	/// <summary>
	/// Janky, but this is used to preserve selections when drag+drop reordering mods.
	/// </summary>
	private HashSet<string> _lastSelected = [];

	/// <summary>
	/// Prevents dropping mods into other mods, and instead makes the drop operation move the mod in the list.
	/// Only ModCategory models should allow dropping mods into them.
	/// </summary>
	/// <param name="e"></param>
	private static void MaybeRedirectDrop(TreeDataGridRowDragEventArgs e)
	{
		if (e.Position == TreeDataGridRowDropPosition.Inside && e.TargetRow.Model is not ModCategory)
		{
			//e.Inner.DragEffects = DragDropEffects.None;
			e.Position = TreeDataGridRowDropPosition.Before;
		}
	}

	private void OnDrag(TreeDataGridRowDragEventArgs e)
	{
		MaybeRedirectDrop(e);
		_lastSelected.Clear();
		if (e.Inner.Data.Get(DragInfo.DataFormat) is DragInfo di && di.Source.Selection is ITreeDataGridRowSelectionModel<IModEntry> selection)
		{
			var mods = selection.SelectedItems;
			foreach (var mod in mods)
			{
				if (mod?.UUID.IsValid() == true) _lastSelected.Add(mod.UUID);
			}
		}
	}

	private void OnDrop(TreeDataGridRowDragEventArgs e)
	{
		MaybeRedirectDrop(e);
	}

	public ModListView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			FilterExpander.GetObservable(Expander.IsExpandedProperty).Skip(1).BindTo(ViewModel, x => x.IsFilterEnabled);

			d(Observable.FromEventPattern<KeyEventArgs>(FilterTextBox, nameof(FilterTextBox.KeyDown)).Subscribe(e =>
			{
				var key = e.EventArgs.Key;
				if (key == Key.Return || key == Key.Enter || key == Key.Escape)
				{
					ModsTreeDataGrid.Focus(NavigationMethod.Pointer);
				}
			}));

			d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>?, TreeDataGridRowDragEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDragOver += h,
				h => ModsTreeDataGrid.RowDragOver -= h
			).Subscribe(OnDrag));

			d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>?, TreeDataGridRowDragEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDrop += h,
				h => ModsTreeDataGrid.RowDrop -= h
			).Subscribe(OnDrop));

			d(Observable.FromEventPattern<ChildIndexChangedEventArgs>(ModsTreeDataGrid.RowsPresenter!, 
				nameof(ModsTreeDataGrid.RowsPresenter.ChildIndexChanged)).Subscribe(e =>
			{
				if(e.EventArgs.Child is TreeDataGridRow row && row.Model is IModEntry mod)
				{
					mod.Index = e.EventArgs.Index;
					if (mod.UUID.IsValid() && _lastSelected.Contains(mod.UUID))
					{
						_lastSelected.Remove(mod.UUID);
						RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(10), () =>
						{
							ModsTreeDataGrid.RowSelection!.Select(mod.Index);
						});
					}
				}
			}));
			

			/*d(Observable.FromEvent<EventHandler<TreeDataGridRowDragStartedEventArgs>?, TreeDataGridRowDragStartedEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDragStarted += h,
				h => ModsTreeDataGrid.RowDragStarted -= h
			).Throttle(TimeSpan.FromMilliseconds(50))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(e =>
			{
				
			}));*/
		});
	}
}
