using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;

using ModManager.Models.Mod;
using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;
public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
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

	public ModListView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
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
			).Subscribe(MaybeRedirectDrop));

			d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>?, TreeDataGridRowDragEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDrop += h,
				h => ModsTreeDataGrid.RowDrop -= h
			).Subscribe(MaybeRedirectDrop));

			d(Observable.FromEventPattern<ChildIndexChangedEventArgs>(ModsTreeDataGrid.RowsPresenter!, nameof(ModsTreeDataGrid.RowsPresenter.ChildIndexChanged)).Subscribe(e =>
			{
				if(e.EventArgs.Child is TreeDataGridRow row && row.Model is IModEntry mod)
				{
					mod.Index = e.EventArgs.Index;
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
