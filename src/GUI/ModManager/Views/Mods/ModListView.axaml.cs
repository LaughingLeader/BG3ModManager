using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

using DynamicData;

using ModManager.Models.Mod;
using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;
public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
	private static IList<IModEntry> GetItems(HierarchicalTreeDataGridSource<IModEntry> from, IndexPath path)
	{
		IEnumerable<IModEntry>? children;

		if (path.Count == 0)
			children = from.Items;
		else if (from.TryGetModelAt(path, out var parent))
			children = ((ITreeDataGridSource)from)!.GetModelChildren(parent)?.Cast<IModEntry>();
		else
			throw new IndexOutOfRangeException();

		if (children is null)
			throw new InvalidOperationException("The requested drop target has no children.");

		return children as IList<IModEntry> ??
			throw new InvalidOperationException("Items does not implement IList<T>.");
	}

	private static void DragDropRows(
			HierarchicalTreeDataGridSource<IModEntry> source,
			HierarchicalTreeDataGridSource<IModEntry> target,
			IEnumerable<IndexPath> indexes,
			IndexPath targetIndex,
			TreeDataGridRowDropPosition position,
			DragDropEffects effects)
	{
		if (effects != DragDropEffects.Move)
			throw new NotSupportedException("Only move is currently supported for drag/drop.");

		if (target.IsSorted)
			throw new NotSupportedException("Drag/drop is not supported on sorted data.");

		IList<IModEntry> targetItems;
		int ti;

		if (position == TreeDataGridRowDropPosition.Inside)
		{
			targetItems = GetItems(target, targetIndex);
			ti = targetItems.Count;
		}
		else
		{
			targetItems = GetItems(target, targetIndex[..^1]);
			ti = targetIndex[^1];
		}

		if (position == TreeDataGridRowDropPosition.After)
			++ti;

		var sourceItems = new List<IModEntry>();

		foreach (var g in indexes.GroupBy(x => x[..^1]))
		{
			var items = GetItems(source, g.Key);

			foreach (var i in g.Select(x => x[^1]).OrderByDescending(x => x))
			{
				sourceItems.Add(items[i]);

				if (items == targetItems && i < ti)
					--ti;

				items.RemoveAt(i);
			}
		}

		for (var si = sourceItems.Count - 1; si >= 0; --si)
		{
			targetItems.Insert(ti++, sourceItems[si]);
		}
	}

	private static TreeDataGridRowDropPosition GetDropPosition(bool allowInside, DragEventArgs e, TreeDataGridRow row)
	{
		var rowY = e.GetPosition(row).Y / row.Bounds.Height;

		if (allowInside)
		{
			if (rowY < 0.33)
				return TreeDataGridRowDropPosition.Before;
			else if (rowY > 0.66)
				return TreeDataGridRowDropPosition.After;
			else
				return TreeDataGridRowDropPosition.Inside;
		}
		else
		{
			if (rowY < 0.5)
				return TreeDataGridRowDropPosition.Before;
			else
				return TreeDataGridRowDropPosition.After;
		}
	}


	/// <summary>
	/// Janky, but this is used to preserve selections when drag+drop reordering mods.
	/// </summary>
	private readonly HashSet<string> _lastSelected = [];

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
			e.Position = GetDropPosition(false, e.Inner, e.TargetRow);
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

			//List to List
			if (e.Position == TreeDataGridRowDropPosition.None && di.Source != ModsTreeDataGrid.Source)
			{
				e.Position = GetDropPosition(e.TargetRow.Model is ModCategory, e.Inner, e.TargetRow);
				e.Inner.DragEffects = DragDropEffects.Move;
			}
		}
	}

	private void OnDrop(TreeDataGridRowDragEventArgs e)
	{
		MaybeRedirectDrop(e);

		//List to List
		if (e.Inner.Data.Get(DragInfo.DataFormat) is DragInfo di
			&& di.Source != ModsTreeDataGrid.Source
			&& di.Source is HierarchicalTreeDataGridSource<IModEntry> listSource
			&& ModsTreeDataGrid.Source is HierarchicalTreeDataGridSource<IModEntry> target)
		{
			if (e.Position == TreeDataGridRowDropPosition.None)
			{
				e.Position = GetDropPosition(e.TargetRow.Model is ModCategory, e.Inner, e.TargetRow);
			}
			var targetIndex = target.Rows.RowIndexToModelIndex(e.TargetRow.RowIndex);
			DragDropRows(listSource, target, listSource.RowSelection!.SelectedIndexes, targetIndex, e.Position, e.Inner.DragEffects);
		}
	}

	private void OnDragStarted(TreeDataGridRowDragStartedEventArgs e)
	{

	}

	private static void OnError(Exception ex)
	{
		DivinityApp.Log($"Error: {ex}");
	}

	public ModListView()
	{
		InitializeComponent();

		if (Design.IsDesignMode) return;

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				d(FilterExpander.GetObservable(Expander.IsExpandedProperty).Skip(1).BindTo(ViewModel, x => x.IsFilterEnabled));

				//Throttle filtering here so we can be sure we're delaying when the user may be typing
				d(FilterTextBox.GetObservable(TextBox.TextProperty)
				.Skip(1)
				.Throttle(TimeSpan.FromMilliseconds(500))
				.ObserveOn(RxApp.MainThreadScheduler)
				.BindTo(ViewModel, x => x.FilterInputText));

				d(ViewModel.WhenAnyValue(x => x.FilterInputText).BindTo(this, x => x.FilterTextBox.Text));
			}

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
			).Subscribe(OnDrag, OnError));

			d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>?, TreeDataGridRowDragEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDrop += h,
				h => ModsTreeDataGrid.RowDrop -= h
			).Subscribe(OnDrop, OnError));

			d(Observable.FromEvent<EventHandler<TreeDataGridRowDragStartedEventArgs>?, TreeDataGridRowDragStartedEventArgs>(
				h => (sender, e) => h(e),
				h => ModsTreeDataGrid.RowDragStarted += h,
				h => ModsTreeDataGrid.RowDragStarted -= h
			).Subscribe(OnDragStarted, OnError));

			d(Observable.FromEventPattern<ChildIndexChangedEventArgs>(ModsTreeDataGrid.RowsPresenter!,
				nameof(ModsTreeDataGrid.RowsPresenter.ChildIndexChanged)).Subscribe(e =>
			{
				if (e.EventArgs.Child is TreeDataGridRow row && row.Model is IModEntry mod)
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
