using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

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

	public static void DragDropRows(
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

	public static TreeDataGridRowDropPosition GetDropPosition(bool allowInside, DragEventArgs e, TreeDataGridRow row)
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
		if (e.Inner.Data.Get(DragInfo.DataFormat) is DragInfo di && di.Source.Selection is ITreeDataGridRowSelectionModel<IModEntry> selection)
		{
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
			foreach (var mod in listSource.RowSelection!.SelectedItems)
			{
				if (mod != null) mod.PreserveSelection = true;
			}

			if (e.Position == TreeDataGridRowDropPosition.None)
			{
				e.Position = GetDropPosition(e.TargetRow.Model is ModCategory, e.Inner, e.TargetRow);
			}
			//Clear the previous selection, so only the dropped items are selected
			target.RowSelection!.Clear();
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

	private void OnPointerDown(object? sender, PointerPressedEventArgs e)
	{
		//Allow deselecting with just left click, if no modifiers are pressed and a single item is selected
		if (e.KeyModifiers == KeyModifiers.None && sender is TreeDataGridRow row && row.IsSelected && ModsTreeDataGrid.RowSelection?.Count == 1)
		{
			ModsTreeDataGrid.RowSelection?.Deselect(row.RowIndex);
			e.Handled = true;
		}
	}

	private void OnRowPrepared(object? sender, TreeDataGridRowEventArgs e)
	{
		e.Row.PointerPressed += OnPointerDown;
	}

	public ModListView()
	{
		InitializeComponent();

		if (Design.IsDesignMode)
		{
			Background = Brushes.Black;
			return;
		}

		ModsTreeDataGrid.RowPrepared += OnRowPrepared;

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

				d(Observable.FromEventPattern<KeyEventArgs>(FilterTextBox, nameof(FilterTextBox.KeyDown)).Subscribe(e =>
				{
					var key = e.EventArgs.Key;
					if (key == Key.Return || key == Key.Enter || key == Key.Escape)
					{
						ModsTreeDataGrid.Focus(NavigationMethod.Pointer);
					}
				}));

				ModsTreeDataGrid.GetObservable(IsFocusedProperty).BindTo(ViewModel, x => x.IsFocused);
				ModsTreeDataGrid.GetObservable(IsKeyboardFocusWithinProperty).BindTo(ViewModel, x => x.IsKeyboardFocusWithin);

				d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>, TreeDataGridRowDragEventArgs>(
					h => (sender, e) => h(e),
					h => ModsTreeDataGrid.RowDragOver += h,
					h => ModsTreeDataGrid.RowDragOver -= h
				).Subscribe(OnDrag, OnError));

				d(Observable.FromEvent<EventHandler<TreeDataGridRowDragEventArgs>, TreeDataGridRowDragEventArgs>(
					h => (sender, e) => h(e),
					h => ModsTreeDataGrid.RowDrop += h,
					h => ModsTreeDataGrid.RowDrop -= h
				).Subscribe(OnDrop, OnError));

				d(Observable.FromEvent<EventHandler<TreeDataGridRowDragStartedEventArgs>, TreeDataGridRowDragStartedEventArgs>(
					h => (sender, e) => h(e),
					h => ModsTreeDataGrid.RowDragStarted += h,
					h => ModsTreeDataGrid.RowDragStarted -= h
				).Subscribe(OnDragStarted, OnError));

				d(Observable.FromEvent<EventHandler<TreeDataGridRowEventArgs>, TreeDataGridRowEventArgs>(
					h => (sender, e) => h(e),
					h => ModsTreeDataGrid.RowPrepared += h,
					h => ModsTreeDataGrid.RowPrepared -= h
				).Subscribe(e =>
				{
					if (e.Row.Model is IModEntry mod && mod.PreserveSelection)
					{
						/*ModEntryTreeDataGridRowSelectionModel preserves the selection after drag + drop visually,
						while this makes sure the backing data stays selected.
						If this isn't set here, then the next drag + drop will only move the directly selected item.
						*/
						ModsTreeDataGrid.RowSelection!.Select(e.RowIndex);
						mod.PreserveSelection = false;
					}
				}));

				d(Observable.FromEvent<EventHandler<ChildIndexChangedEventArgs>, ChildIndexChangedEventArgs>(
					h => (sender, e) => h(e),
					h => ModsTreeDataGrid.RowsPresenter!.ChildIndexChanged += h,
					h => ModsTreeDataGrid.RowsPresenter!.ChildIndexChanged -= h
				).Subscribe(e =>
				{
					if (e.Index > -1 && e.Child is TreeDataGridRow row && row.Model is IModEntry mod)
					{
						//var index = ModsTreeDataGrid.Rows!.RowIndexToModelIndex(e.Index);
						mod.Index = e.Index;
					}
				}));

				d(ViewModel.FocusCommand.Subscribe(e =>
				{
					ModsTreeDataGrid.Focus(NavigationMethod.Pointer);
				}));
			}
		});
	}
}
