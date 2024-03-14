using DivinityModManager.Controls.Views;
using DivinityModManager.Converters;
using DivinityModManager.Models;
using DivinityModManager.Util;
using DivinityModManager.ViewModels;
using DivinityModManager.ViewModels.Main;

using DynamicData.Binding;

using GongSolutions.Wpf.DragDrop;
using GongSolutions.Wpf.DragDrop.Utilities;

using ReactiveUI;

using System.ComponentModel;
using System.IO.Packaging;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DivinityModManager.Views.Main;

public interface IModViewLayout
{
	void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null);
	void SelectMods(IEnumerable<ISelectable> dataList, bool activeMods);
	void FixActiveModsScrollbar();
}

public class HorizontalModLayoutBase : ReactiveUserControl<ModOrderViewModel> { }

/// <summary>
/// Interaction logic for HorizonalModLayout.xaml
/// </summary>
public partial class HorizontalModLayout : HorizontalModLayoutBase, IModViewLayout
{
	private object _focusedList = null;

	private bool ListHasFocus(ListView listView)
	{
		if (_focusedList == listView || listView.IsFocused || listView.IsKeyboardFocused)
		{
			return true;
		}
		if (listView.SelectedItem is ListViewItem item && (item.IsFocused || item.IsKeyboardFocused))
		{
			return true;
		}
		return false;
	}

	private bool FocusSelectedItem(ListView lv)
	{
		try
		{
			var listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(lv.SelectedItem);
			if (listBoxItem == null)
			{
				var firstItem = lv.Items.GetItemAt(0);
				if (firstItem != null)
				{
					listBoxItem = (ListBoxItem)lv.ItemContainerGenerator.ContainerFromItem(firstItem);
				}
			}
			if (listBoxItem != null)
			{
				listBoxItem.Focus();
				Keyboard.Focus(listBoxItem);
				return true;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"{ex}");
		}
		return false;
	}

	private void FocusList(ListView listView)
	{
		if (!FocusSelectedItem(listView))
		{
			listView.Focus();
		}
	}

	private void TraceBackground(Control c)
	{
		DivinityApp.Log($"{c} Background({c.Background})");
		foreach (var c2 in c.FindVisualChildren<Control>())
		{
			if (c2.Background != null && !c2.Background.Equals(Brushes.Transparent))
			{
				TraceBackground(c2);
			}
		}
	}

	private void SetupListView(ListView listView)
	{
		listView.InputBindings.Add(new KeyBinding(ApplicationCommands.SelectAll, new KeyGesture(Key.A, ModifierKeys.Control)));
		listView.CommandBindings.Add(new CommandBinding(ApplicationCommands.SelectAll, (_sender, _e) =>
		{
			listView.SelectAll();
		}));

		listView.InputBindings.Add(new KeyBinding(ReactiveCommand.Create(() =>
		{
			listView.SelectedItems.Clear();

		}), new KeyGesture(Key.D, ModifierKeys.Control)));

		listView.ItemContainerStyle = this.FindResource("ListViewItemMouseEvents") as Style;
		listView.GotFocus += (object sender, RoutedEventArgs e) =>
		{
			_focusedList = sender;
		};
		listView.LostFocus += (object sender, RoutedEventArgs e) =>
		{
			if (_focusedList == sender)
			{
				_focusedList = null;
			}
		};
	}

	public void FixActiveModsScrollbar()
	{
		if (ActiveModsListView.FindVisualChildren<ScrollViewer>().FirstOrDefault() is ScrollViewer sv)
		{
			sv.ScrollToHorizontalOffset(0d);
		}
	}

	public void UpdateViewSelection(IEnumerable<ISelectable> dataList, ListView listView = null)
	{
		if (dataList != null)
		{
			if (listView == null)
			{
				if (dataList == ViewModel.ActiveMods)
				{
					listView = ActiveModsListView;
				}
				else if (dataList == ViewModel.InactiveMods)
				{
					listView = InactiveModsListView;
				}
				else if (dataList == Services.Mods.ForceLoadedMods)
				{
					listView = ForceLoadedModsListView;
				}
			}

			if (listView != null && dataList.Count() > 0)
			{
				IInputElement focusedItem = FocusManager.GetFocusedElement(listView);
				foreach (var mod in dataList)
				{
					var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
					if (listItem != null)
					{
						if (mod.Visibility == Visibility.Visible)
						{
							listItem.IsSelected = mod.IsSelected;
							if (listView.IsFocused && focusedItem == null && mod.IsSelected)
							{
								focusedItem = listItem;
								FocusManager.SetFocusedElement(listView, focusedItem);
							}
						}
						else
						{
							listItem.IsSelected = false;
						}
					}
				}
			}
		}
	}

	public void SelectMods(IEnumerable<ISelectable> dataList, bool activeMods)
	{
		if (dataList != null)
		{
			var listView = activeMods ? ActiveModsListView : InactiveModsListView;
			foreach (var mod in dataList)
			{
				var listItem = (ListViewItem)listView.ItemContainerGenerator.ContainerFromItem(mod);
				if (listItem != null)
				{
					listItem.IsSelected = mod.Visibility == Visibility.Visible;
				}
			}
		}
	}

	private void UpdateIsSelected(SelectionChangedEventArgs e, IEnumerable<DivinityModData> list)
	{
		if (e != null && list != null)
		{
			var targetUUIDs = list.Select(x => x.UUID).ToHashSet();

			if (e.RemovedItems != null && e.RemovedItems.Count > 0)
			{
				foreach (var removedItem in e.RemovedItems.Cast<DivinityModData>())
				{
					if (targetUUIDs.Contains(removedItem.UUID))
					{
						removedItem.IsSelected = false;
					}
				}
			}

			if (e.AddedItems != null && e.AddedItems.Count > 0)
			{
				foreach (var addedItem in e.AddedItems.Cast<DivinityModData>())
				{
					addedItem.IsSelected = true;
				}
			}
		}
	}

	private IDisposable updatingActiveViewSelection;
	private IDisposable updatingInactiveViewSelection;
	private IDisposable updatingForcedViewSelection;

	private void ActiveModListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (ActiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			updatingActiveViewSelection?.Dispose();
			updatingActiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
			{
				UpdateViewSelection(ViewModel.ActiveMods, ActiveModsListView);
			});
		}
	}

	private void InactiveModListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (InactiveModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			updatingInactiveViewSelection?.Dispose();
			updatingInactiveViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
			{
				UpdateViewSelection(ViewModel.InactiveMods, InactiveModsListView);
			});
		}
	}

	private void ForceLoadedModsListView_ItemContainerStatusChanged(EventArgs e)
	{
		if (ForceLoadedModsListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
		{
			updatingForcedViewSelection?.Dispose();
			updatingForcedViewSelection = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(25), () =>
			{
				UpdateViewSelection(Services.Mods.ForceLoadedMods, ForceLoadedModsListView);
			});
		}
	}

	private IDisposable _updateScroll;

	private void MoveSelectedMods()
	{
		if (ListHasFocus(ActiveModsListView))
		{
			var selectedMods = ViewModel.ActiveMods.Where(x => x.IsSelected).ToList();

			if (selectedMods.Count <= 0) return;

			var selectedMod = selectedMods.FirstOrDefault();
			var nextSelectedIndex = ViewModel.ActiveMods.IndexOf(selectedMod);

			var scrollTargetIndex = InactiveModsListView.SelectedIndex;
			var dropInfo = new ManualDropInfo(selectedMods, InactiveModsListView.SelectedIndex, InactiveModsListView, ViewModel.InactiveMods, ViewModel.ActiveMods);
			InactiveModsListView.UnselectAll();
			ViewModel.DropHandler.Drop(dropInfo);
			string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
			string text = $"Moved {selectedMods.Count} {countSuffix} to the inactive mods list.";
			ScreenReaderHelper.Speak(text);
			DivinityApp.ShowAlert(text, AlertType.Info, 10);
			ViewModel.CanMoveSelectedMods = false;

			if (Services.Settings.ManagerSettings.ShiftListFocusOnSwap)
			{
				InactiveModsListView.Focus();
			}

			_updateScroll?.Dispose();

			_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
			{
				//InactiveModsListView.UpdateLayout();
				if (scrollTargetIndex <= 0)
				{
					ScrollToTop(InactiveModsListView);
				}
				else if (scrollTargetIndex >= InactiveModsListView.Items.Count)
				{
					ScrollToBottom(InactiveModsListView);
				}
				else
				{
					ScrollToMod(InactiveModsListView, selectedMod);
					//FocusMod(InactiveModsListView, selectedMod);
				}

				if (nextSelectedIndex >= ViewModel.ActiveMods.Count)
				{
					nextSelectedIndex = ViewModel.ActiveMods.Count - 1;
				}

				ActiveModsListView.SelectedIndex = nextSelectedIndex;
				//FocusMod(ActiveModsListView, ActiveModsListView.SelectedItem);
			});
		}
		else if (ListHasFocus(InactiveModsListView))
		{
			var selectedMods = ViewModel.InactiveMods.Where(x => x.IsSelected).ToList();

			if (selectedMods.Count <= 0) return;

			var selectedMod = selectedMods.FirstOrDefault();
			var nextSelectedIndex = ViewModel.InactiveMods.IndexOf(selectedMod);

			var scrollTargetIndex = ActiveModsListView.SelectedIndex;
			var dropInfo = new ManualDropInfo(selectedMods, ActiveModsListView.SelectedIndex, ActiveModsListView, ViewModel.ActiveMods, ViewModel.InactiveMods);
			ActiveModsListView.UnselectAll();
			ViewModel.DropHandler.Drop(dropInfo);

			string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
			string text = $"Moved {selectedMods.Count} {countSuffix} to the active mods list.";
			ScreenReaderHelper.Speak(text);
			DivinityApp.ShowAlert(text, AlertType.Info, 10);
			ViewModel.CanMoveSelectedMods = false;

			if (Services.Settings.ManagerSettings.ShiftListFocusOnSwap)
			{
				ActiveModsListView.Focus();
			}

			_updateScroll?.Dispose();

			_updateScroll = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
			{
				//ActiveModsListView.UpdateLayout();
				if (scrollTargetIndex <= 0)
				{
					ScrollToTop(ActiveModsListView);
				}
				else if (scrollTargetIndex >= ActiveModsListView.Items.Count)
				{
					ScrollToBottom(ActiveModsListView);
				}
				else
				{
					ScrollToMod(ActiveModsListView, selectedMod);
					//FocusMod(ActiveModsListView, selectedMod);
				}

				if (nextSelectedIndex >= ViewModel.InactiveMods.Count)
				{
					nextSelectedIndex = ViewModel.InactiveMods.Count - 1;
				}

				InactiveModsListView.SelectedIndex = nextSelectedIndex;
				//FocusMod(InactiveModsListView, InactiveModsListView.SelectedItem);
			});
		}
	}

	public void FocusInitialActiveSelected()
	{
		if (Services.Mods.ActiveSelected <= 0)
		{
			ActiveModsListView.SelectedIndex = 0;
		}
		try
		{
			ListViewItem item = (ListViewItem)ActiveModsListView.ItemContainerGenerator.ContainerFromItem(ActiveModsListView.SelectedItem);
			if (item != null)
			{
				Keyboard.Focus(item);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error focusing selected item:{ex}");
		}
	}

	public bool FocusMod(ModListView modListView, object mod)
	{
		if (modListView.ItemContainerGenerator.ContainerFromItem(mod) is ListViewItem item)
		{
			FocusManager.SetFocusedElement(modListView, item);
			//item.BringIntoView();
			return true;
		}
		return false;
	}

	public void ScrollToMod(ModListView modListView, DivinityModData mod)
	{
		var index = modListView.Items.IndexOf(mod);
		if (index > -1)
		{
			modListView.UpdateLayout();
			modListView.ScrollIntoView(modListView.Items[index]);
		}
	}

	public void ScrollToTop(ModListView modListView)
	{
		if (modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToTop();
		}
	}

	public void ScrollToBottom(ModListView modListView)
	{
		if (modListView.GetVisualDescendent<ScrollViewer>() is ScrollViewer scrollViewer)
		{
			scrollViewer.ScrollToBottom();
		}
	}

	private GridLength? _lastActiveModsRowHeight;
	private GridLength? _lastActiveModsSplitterHeight;
	private GridLength? _lastActiveModsOverridesHeight;

	public void SaveLayout()
	{
		_lastActiveModsRowHeight = ActiveModsListRow.Height;
		_lastActiveModsSplitterHeight = ActiveModsListGridRow.Height;
		_lastActiveModsOverridesHeight = ActiveModsListForcedModsRow.Height;
	}

	public void RestoreLayout()
	{
		if (_lastActiveModsRowHeight != null)
		{
			ActiveModsListRow.Height = _lastActiveModsRowHeight.Value;
			ActiveModsListGridRow.Height = _lastActiveModsSplitterHeight.Value;
			ActiveModsListForcedModsRow.Height = _lastActiveModsOverridesHeight.Value;
		}
	}

	public HorizontalModLayout()
	{
		InitializeComponent();

		SetupListView(ActiveModsListView);
		SetupListView(InactiveModsListView);

		bool setInitialFocus = true;

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				var dragBinding = new Binding(nameof(ModOrderViewModel.DragHandler)) { Source = ViewModel };
				var dropBinding = new Binding(nameof(ModOrderViewModel.DropHandler)) { Source = ViewModel };

				ActiveModsListView.SetBinding(GongSolutions.Wpf.DragDrop.DragDrop.DragHandlerProperty, dragBinding);
				InactiveModsListView.SetBinding(GongSolutions.Wpf.DragDrop.DragDrop.DragHandlerProperty, dragBinding);
				ActiveModsListView.SetBinding(GongSolutions.Wpf.DragDrop.DragDrop.DropHandlerProperty, dropBinding);
				InactiveModsListView.SetBinding(GongSolutions.Wpf.DragDrop.DragDrop.DropHandlerProperty, dropBinding);

				d(this.Events().KeyUp.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(key =>
				{
					if (key == ViewModel.Keys.Confirm.Key)
					{
						ViewModel.CanMoveSelectedMods = true;
					}
				}));
				d(this.Events().KeyDown.Select(e => e.Key != Key.System ? e.Key : e.SystemKey).Subscribe(key =>
				{
					switch (key)
					{
						case Key.Up:
						case Key.Right:
						case Key.Down:
						case Key.Left:
							DivinityApp.IsKeyboardNavigating = true;
							break;
					}
					HorizontalModLayout_KeyDown(key);
				}));
				d(this.Events().LostFocus.Subscribe((e) => ViewModel.CanMoveSelectedMods = true));
				d(this.Events().Loaded.ObserveOn(RxApp.MainThreadScheduler).Subscribe((e) =>
				{
					if (setInitialFocus)
					{
						this.ActiveModsListView.Focus();
						setInitialFocus = false;
					}
				}));

				d(this.ActiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ActiveModListView_ItemContainerStatusChanged));
				d(this.InactiveModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(InactiveModListView_ItemContainerStatusChanged));
				d(this.ForceLoadedModsListView.ItemContainerGenerator.Events().StatusChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(ForceLoadedModsListView_ItemContainerStatusChanged));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(ActiveModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, ViewModel.ActiveMods);
				}));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(InactiveModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, ViewModel.InactiveMods);
				}));

				d(Observable.FromEventPattern<SelectionChangedEventArgs>(ForceLoadedModsListView, "SelectionChanged")
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe((e) =>
				{
					UpdateIsSelected(e.EventArgs, Services.Mods.ForceLoadedMods);
				}));

				d(ViewModel.OrderJustLoadedCommand.ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
				{
					AutoSizeNameColumn_ActiveMods();
					AutoSizeNameColumn_InactiveMods();
				}));

				ViewModel.Layout = this;

				var modsService = Services.Mods;

				d(this.OneWayBind(ViewModel, vm => vm.ActiveMods, v => v.ActiveModsListView.ItemsSource));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveMods, v => v.InactiveModsListView.ItemsSource));
				d(this.OneWayBind(ViewModel, vm => vm.ForceLoadedMods, v => v.ForceLoadedModsListView.ItemsSource));

				d(this.OneWayBind(ViewModel, vm => vm.OverrideModsVisibility, v => v.ForceLoadedModsListView.Visibility));
				d(this.OneWayBind(ViewModel, vm => vm.OverrideModsVisibility, v => v.ActiveModListViewGridSplitter.Visibility));

				d(this.Bind(ViewModel, vm => vm.ActiveModFilterText, v => v.ActiveModsFilterTextBox.Text));
				d(this.Bind(ViewModel, vm => vm.InactiveModFilterText, v => v.InactiveModsFilterTextBox.Text));
				d(this.Bind(ViewModel, vm => vm.OverrideModsFilterText, v => v.OverrideModsFilterTextBox.Text));

				d(this.OneWayBind(ViewModel, vm => vm.ActiveModsFilterResultText, v => v.ActiveModsFilterResultText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveModsFilterResultText, v => v.InactiveModsFilterResultText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.OverrideModsFilterResultText, v => v.OverrideModsFilterResultText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.TotalActiveModsHidden, v => v.ActiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));
				d(this.OneWayBind(ViewModel, vm => vm.TotalInactiveModsHidden, v => v.InactiveModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));
				d(this.OneWayBind(ViewModel, vm => vm.TotalOverrideModsHidden, v => v.OverrideModsFilterResultText.Visibility, IntToVisibilityConverter.FromInt));

				d(this.OneWayBind(ViewModel, vm => vm.ActiveSelectedText, v => v.ActiveSelectedText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.InactiveSelectedText, v => v.InactiveSelectedText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.OverrideModsSelectedText, v => v.OverrideModsSelectedText.Text));
				d(modsService.WhenAnyValue(x => x.ActiveSelected).Select(PropertyConverters.IntToVisibility).BindTo(this, x => x.ActiveSelectedText.Visibility));
				d(modsService.WhenAnyValue(x => x.InactiveSelected).Select(PropertyConverters.IntToVisibility).BindTo(this, x => x.InactiveSelectedText.Visibility));
				d(modsService.WhenAnyValue(x => x.OverrideModsSelected).Select(PropertyConverters.IntToVisibility).BindTo(this, x => x.OverrideModsSelectedText.Visibility));

				var gridLengthConverter = new GridLengthConverter();
				var zeroHeight = (GridLength)gridLengthConverter.ConvertFrom(0);
				var forceModsHeight = (GridLength)gridLengthConverter.ConvertFrom("2*");

				d(ViewModel.WhenAnyValue(x => x.OverrideModsVisibility).ObserveOn(RxApp.MainThreadScheduler).Subscribe(visibility =>
				{
					foreach (var row in this.ActiveModListGrid.RowDefinitions.Where(x => x.Name != "ActiveModsListRow"))
					{
						if (visibility == Visibility.Visible)
						{
							if (row.Name == "ActiveModsListGridRow")
							{
								row.Height = GridLength.Auto;
							}
							else if (row.Name == "ActiveModsListForcedModsRow")
							{
								row.Height = forceModsHeight;
							}
						}
						else
						{
							row.Height = zeroHeight;
						}
					}
				}));

				ViewModel.Keys.MoveFocusLeft.AddAction(() =>
				{
					DivinityApp.IsKeyboardNavigating = true;
					this.ActiveModsListView.Focus();

					if (modsService.ActiveSelected <= 0)
					{
						ActiveModsListView.SelectedIndex = 0;
					}

					//InactiveModsListView.UnselectAll();
					FocusList(ActiveModsListView);
				});

				ViewModel.Keys.MoveFocusRight.AddAction(() =>
				{
					DivinityApp.IsKeyboardNavigating = true;
					InactiveModsListView.Focus();
					if (modsService.ActiveSelected <= 0)
					{
						InactiveModsListView.SelectedIndex = 0;
					}
					//ActiveModsListView.UnselectAll();
					FocusList(InactiveModsListView);
				});


				ViewModel.Keys.SwapListFocus.AddAction(() =>
				{
					if (ListHasFocus(InactiveModsListView))
					{
						DivinityApp.IsKeyboardNavigating = true;
						FocusList(ActiveModsListView);
					}
					else if (ListHasFocus(ActiveModsListView))
					{
						DivinityApp.IsKeyboardNavigating = true;
						FocusList(InactiveModsListView);
					}
				});

				//InactiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveLeftCommand, new KeyGesture(Key.Left)));
				ViewModel.Keys.ToggleFilterFocus.AddAction(() =>
				{
					if (ListHasFocus(ActiveModsListView))
					{
						if (!this.ActiveModsFilterTextBox.IsFocused)
						{
							this.ActiveModsFilterTextBox.Focus();
						}
						else
						{
							FocusSelectedItem(ActiveModsListView);
						}
					}
					else
					{
						if (!this.InactiveModsFilterTextBox.IsFocused)
						{
							this.InactiveModsFilterTextBox.Focus();
						}
						else
						{
							FocusSelectedItem(InactiveModsListView);
						}
					}
				});

				//ActiveModsListView.InputBindings.Add(new InputBinding(ViewModel.MoveRightCommand, new KeyGesture(Key.Right)));

				d(modsService.WhenAnyValue(x => x.ActiveSelected).Subscribe((c) =>
				{
					if (c > 1 && DivinityApp.IsScreenReaderActive())
					{
						var peer = UIElementAutomationPeer.FromElement(this.ActiveSelectedText) ?? UIElementAutomationPeer.CreatePeerForElement(this.ActiveSelectedText);
						peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
					}
				}));

				d(modsService.WhenAnyValue(x => x.InactiveSelected).Subscribe((c) =>
				{
					if (c > 1 && DivinityApp.IsScreenReaderActive())
					{
						var peer = UIElementAutomationPeer.FromElement(this.InactiveSelectedText) ?? UIElementAutomationPeer.CreatePeerForElement(this.InactiveSelectedText);
						peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
					}
				}));

				d(modsService.WhenAnyValue(x => x.OverrideModsSelected).Subscribe((c) =>
				{
					if (c > 1 && DivinityApp.IsScreenReaderActive())
					{
						var peer = UIElementAutomationPeer.FromElement(this.OverrideModsSelectedText) ?? UIElementAutomationPeer.CreatePeerForElement(this.OverrideModsSelectedText);
						peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
					}
				}));
			}
			//BindingHelper.CreateCommandBinding(ViewModel.View.EditFocusActiveListMenuItem, "MoveLeftCommand", ViewModel);
		});
	}

	private void HorizontalModLayout_KeyDown(Key key)
	{
		var keyIsDown = key == ViewModel.Keys.Confirm.Key && (ViewModel.Keys.Confirm.Modifiers == ModifierKeys.None || Keyboard.Modifiers.HasFlag(ViewModel.Keys.Confirm.Modifiers));
		if (!keyIsDown && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
		{
			if (key == Key.Right && ActiveModsListView.IsKeyboardFocusWithin)
			{
				keyIsDown = true;
			}
			else if (key == Key.Left && InactiveModsListView.IsKeyboardFocusWithin)
			{
				keyIsDown = true;
			}
		}
		if (ViewModel.CanMoveSelectedMods && keyIsDown)
		{
			DivinityApp.IsKeyboardNavigating = true;
			var modsService = Services.Mods;
			if (modsService.ActiveSelected > 0 || modsService.InactiveSelected > 0)
			{
				MoveSelectedMods();
			}
		}
	}

	GridViewColumnHeader _lastHeaderClicked = null;
	ListSortDirection _lastDirection = ListSortDirection.Ascending;

	private void ListView_Click(object sender, RoutedEventArgs e)
	{
		ListSortDirection direction;

		if (e.OriginalSource is GridViewColumnHeader headerClicked)
		{
			if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
			{
				if (headerClicked != _lastHeaderClicked)
				{
					direction = ListSortDirection.Ascending;
				}
				else
				{
					if (_lastDirection == ListSortDirection.Ascending)
					{
						direction = ListSortDirection.Descending;
					}
					else
					{
						direction = ListSortDirection.Ascending;
					}
				}

				string header = "";

				if (headerClicked.Column.Header is TextBlock textBlock)
				{
					header = textBlock.Text;
				}
				else if (headerClicked.Column.Header is string gridHeader)
				{
					header = gridHeader;
				}

				Sort(header, direction, sender);

				_lastHeaderClicked = headerClicked;
				_lastDirection = direction;
			}
		}
	}

	private void Sort(string sortBy, ListSortDirection direction, object sender)
	{
		if (sortBy == "Version") sortBy = "Version.Version";
		if (sortBy == "#") sortBy = "Index";
		if (sortBy == "Name") sortBy = "DisplayName";
		if (sortBy == "Modes") sortBy = "Targets";
		if (sortBy == "Last Updated") sortBy = "LastUpdated";

		try
		{
			ListView lv = sender as ListView;
			ICollectionView dataView =
			  CollectionViewSource.GetDefaultView(lv.ItemsSource);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error sorting mods:");
			DivinityApp.Log(ex.ToString());
		}
	}

	private const int _FontSizeMeasurePadding = 48;

	public void AutoSizeNameColumn_ActiveMods()
	{
		if (ViewModel == null || ActiveModsListView.UserResizedColumns) return;
		var count = Math.Max(ViewModel.ActiveMods.Count, Services.Mods.ForceLoadedMods.Count);
		if (count > 0 && ActiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
		{
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
			{
				count = Math.Max(ViewModel.ActiveMods.Count, Services.Mods.ForceLoadedMods.Count);
				if (count > 0)
				{
					var longestName = ViewModel.ActiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name ?? "";
					var longestOverrideName = Services.Mods.ForceLoadedMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name ?? "";

					var sortName = longestName;
					if (!String.IsNullOrEmpty(longestOverrideName) && longestOverrideName.Length > longestName.Length)
					{
						sortName = longestOverrideName;
					}

					if (!String.IsNullOrEmpty(sortName))
					{
						//DivinityApp.LogMessage($"Autosizing active mods grid for name {longestName}");
						var targetWidth = ElementHelper.MeasureText(ActiveModsListView, sortName,
							ActiveModsListView.FontFamily,
							ActiveModsListView.FontStyle,
							ActiveModsListView.FontWeight,
							ActiveModsListView.FontStretch,
							ActiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
						if (Math.Abs(gridView.Columns[1].Width - targetWidth) >= 30)
						{
							ActiveModsListView.Resizing = true;
							gridView.Columns[1].Width = targetWidth;
						}
					}
				}
			});
		}
	}

	public void AutoSizeNameColumn_InactiveMods()
	{
		if (ViewModel == null || InactiveModsListView.UserResizedColumns) return;
		if (ViewModel.InactiveMods.Count > 0 && InactiveModsListView.View is GridView gridView && gridView.Columns.Count >= 2)
		{
			var longestName = ViewModel.InactiveMods.OrderByDescending(m => m.Name.Length).FirstOrDefault()?.Name;
			if (!String.IsNullOrEmpty(longestName))
			{
				InactiveModsListView.Resizing = true;
				//DivinityApp.LogMessage($"Autosizing inactive mods grid for name {longestName}");
				gridView.Columns[0].Width = ElementHelper.MeasureText(InactiveModsListView, longestName,
					InactiveModsListView.FontFamily,
					InactiveModsListView.FontStyle,
					InactiveModsListView.FontWeight,
					InactiveModsListView.FontStretch,
					InactiveModsListView.FontSize).Width + _FontSizeMeasurePadding;
			}
		}
	}

	private void ListViewItem_ModifySelection(object sender, MouseButtonEventArgs e)
	{
		//Fix for when virtualization is enabled, and selected entries outside the view don't get deselected
		if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
		{
			if (sender is ListViewItem listViewitem)
			{
				if (listViewitem.DataContext is DivinityModData modData)
				{
					if (modData.IsActive)
					{
						foreach (var x in ViewModel.ActiveMods)
						{
							if (x != modData && x.IsSelected) x.IsSelected = false;
						}
					}
					else
					{
						foreach (var x in ViewModel.InactiveMods)
						{
							if (x != modData && x.IsSelected) x.IsSelected = false;
						}
					}
				}
			}
		}
	}
}
