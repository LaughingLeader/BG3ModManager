using AdonisUI;

using DivinityModManager.Converters;
using DivinityModManager.Models.App;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.ViewModels;
using DivinityModManager.Views.Main;
using DivinityModManager.Windows;

using ReactiveUI;

using System.Data;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DivinityModManager.Views;

public class MainViewControlViewBase : ReactiveUserControl<MainWindowViewModel> { }

public partial class MainViewControl : MainViewControlViewBase
{
	private readonly MainWindow main;

	private readonly Dictionary<string, MenuItem> menuItems = [];
	public Dictionary<string, MenuItem> MenuItems => menuItems;

	private void RegisterKeyBindings()
	{
		foreach (var key in ViewModel.Keys.All)
		{
			var keyBinding = new KeyBinding(key.Command, key.Key, key.Modifiers);
			BindingOperations.SetBinding(keyBinding, InputBinding.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source = key });
			BindingOperations.SetBinding(keyBinding, KeyBinding.KeyProperty, new Binding { Path = new PropertyPath("Key"), Source = key });
			BindingOperations.SetBinding(keyBinding, KeyBinding.ModifiersProperty, new Binding { Path = new PropertyPath("Modifiers"), Source = key });
			main.InputBindings.Add(keyBinding);
		}

		//Initial keyboard focus by hitting up or down
		var setInitialFocusCommand = ReactiveCommand.Create(() =>
		{
			var modManager = Services.Mods;
			if (!DivinityApp.IsKeyboardNavigating && modManager.ActiveSelected == 0 && modManager.InactiveSelected == 0)
			{
				Services.Get<ModOrderView>()?.ModLayout?.FocusInitialActiveSelected();
			}
		});
		main.InputBindings.Add(new KeyBinding(setInitialFocusCommand, Key.Up, ModifierKeys.None));
		main.InputBindings.Add(new KeyBinding(setInitialFocusCommand, Key.Down, ModifierKeys.None));

		foreach (var item in TopMenuBar.Items)
		{
			if (item is MenuItem entry)
			{
				if (entry.Header is string label)
				{
					menuItems.Add(label, entry);
				}
				else if (!String.IsNullOrWhiteSpace(entry.Name))
				{
					menuItems.Add(entry.Name, entry);
				}
			}
		}

		//Generating menu items
		var menuKeyProperties = typeof(AppKeys)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(MenuSettingsAttribute)))
		.Select(prop => typeof(AppKeys).GetProperty(prop.Name));
		foreach (var prop in menuKeyProperties)
		{
			Hotkey key = (Hotkey)prop.GetValue(ViewModel.Keys);
			MenuSettingsAttribute menuSettings = prop.GetCustomAttribute<MenuSettingsAttribute>();
			if (String.IsNullOrEmpty(key.DisplayName))
				key.DisplayName = menuSettings.DisplayName;

			if (!menuItems.TryGetValue(menuSettings.Parent, out MenuItem parentMenuItem))
			{
				parentMenuItem = new MenuItem
				{
					Header = menuSettings.Parent
				};
				TopMenuBar.Items.Add(parentMenuItem);
				menuItems.Add(menuSettings.Parent, parentMenuItem);
			}

			MenuItem newEntry = new()
			{
				Header = menuSettings.DisplayName,
				InputGestureText = key.ToString(),
				Command = key.Command
			};
			BindingOperations.SetBinding(newEntry, MenuItem.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source = key });
			parentMenuItem.Items.Add(newEntry);
			if (!String.IsNullOrWhiteSpace(menuSettings.ToolTip))
			{
				newEntry.ToolTip = menuSettings.ToolTip;
			}
			if (!String.IsNullOrWhiteSpace(menuSettings.Style))
			{
				Style style = (Style)TryFindResource(menuSettings.Style);
				if (style != null)
				{
					newEntry.Style = style;
				}
			}

			if (menuSettings.AddSeparator)
			{
				parentMenuItem.Items.Add(new Separator());
			}

			menuItems.Add(prop.Name, newEntry);
		}
	}

	protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
	{
		return new CachedAutomationPeer(this);
	}

	public void UpdateColorTheme(bool darkMode)
	{
		ResourceLocator.SetColorScheme(this.Resources, !darkMode ? MainWindow.LightTheme : MainWindow.DarkTheme);
		main.UpdateColorTheme(darkMode);
	}

	private void ComboBox_KeyDown_LoseFocus(object sender, KeyEventArgs e)
	{
		bool loseFocus = false;
		if ((e.Key == Key.Enter || e.Key == Key.Return))
		{
			UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;
			elementWithFocus.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
			ViewModel.StopRenaming(false);
			loseFocus = true;
			e.Handled = true;
		}
		else if (e.Key == Key.Escape)
		{
			ViewModel.StopRenaming(true);
			loseFocus = true;
		}

		if (loseFocus && sender is ComboBox comboBox)
		{
			var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
			tb?.Select(0, 0);
		}
	}

	private void OrdersComboBox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is ComboBox comboBox && ViewModel.IsRenamingOrder)
		{
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), _ =>
			{
				var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
				if (tb != null && !tb.IsFocused)
				{
					var cancel = String.IsNullOrEmpty(tb.Text);
					ViewModel.StopRenaming(cancel);
					if (!cancel)
					{
						ViewModel.SelectedModOrder.Name = tb.Text;
						var directory = Path.GetDirectoryName(ViewModel.SelectedModOrder.FilePath);
						var ext = Path.GetExtension(ViewModel.SelectedModOrder.FilePath);
						string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Join(ViewModel.SelectedModOrder.Name + ext), '_');
						ViewModel.SelectedModOrder.FilePath = Path.Join(directory, outputName);
						AlertBar.SetSuccessAlert($"Renamed load order name/path to '{ViewModel.SelectedModOrder.FilePath}'", 20);
					}
				}
			});
		}
	}

	private void OrderComboBox_OnUserClick(object sender, MouseButtonEventArgs e)
	{
		RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(200), () =>
		{
			if (ViewModel.Settings != null && ViewModel.Settings.LastOrder != ViewModel.SelectedModOrder.Name)
			{
				ViewModel.Settings.LastOrder = ViewModel.SelectedModOrder.Name;
				ViewModel.SaveSettings();
			}
		});
	}

	private void OrdersComboBox_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is ComboBox ordersComboBox)
		{
			var tb = ordersComboBox.FindVisualChildren<TextBox>().FirstOrDefault();
			if (tb != null)
			{
				tb.ContextMenu = ordersComboBox.ContextMenu;
				tb.ContextMenu.DataContext = ViewModel;
			}
		}
	}

	private readonly Dictionary<string, string> _shortcutButtonBindings = new()
	{
		["OpenWorkshopFolderButton"] = "Keys.OpenWorkshopFolder.Command",
		["OpenModsFolderButton"] = "Keys.OpenModsFolder.Command",
		["OpenExtenderLogsFolderButton"] = "Keys.OpenLogsFolder.Command",
		["OpenGameButton"] = "Keys.LaunchGame.Command",
		["LoadGameMasterModOrderButton"] = "Keys.ImportOrderFromSelectedGMCampaign.Command",
	};

	private void ModOrderPanel_Loaded(object sender, RoutedEventArgs e)
	{
		if (sender is Grid orderPanel)
		{
			var buttons = orderPanel.FindVisualChildren<Button>();
			foreach (var button in buttons)
			{
				if (_shortcutButtonBindings.TryGetValue(button.Name, out string path))
				{
					if (button.Command == null)
					{
						BindingHelper.CreateCommandBinding(button, path, ViewModel);
					}
				}
			}
		};
	}

	private void GameMasterCampaignComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		ViewModel.UserChangedSelectedGMCampaign = true;
	}

	private bool PathExists(string path) => !String.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path));

	public void OnActivated()
	{
		this.WhenAnyValue(x => x.ViewModel.MainProgressIsActive).Take(1).Delay(TimeSpan.FromMilliseconds(25)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(b =>
		{
			this.MainBusyIndicator.Visibility = Visibility.Visible;
		});
		this.OneWayBind(ViewModel, vm => vm.HideModList, view => view.ModListRectangle.Visibility, BoolToVisibilityConverter.FromBool);
		this.OneWayBind(ViewModel, vm => vm.MainProgressIsActive, view => view.MainBusyIndicator.IsBusy);

		this.OneWayBind(ViewModel, vm => vm.StatusBarRightText, view => view.StatusBarLoadingOperationTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.NexusModsLimitsText, view => view.StatusBarNexusLimitsTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.NexusModsProfileAvatarVisibility, view => view.NexusModsProfileImage.Visibility);
		this.OneWayBind(ViewModel, vm => vm.NexusModsProfileBitmapImage, view => view.NexusModsProfileImage.Source);

		this.OneWayBind(ViewModel, vm => vm.ModUpdatesAvailable, view => view.UpdatesButtonPanel.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.UpdatingBusyIndicatorVisibility, view => view.UpdatesToggleButtonBusyIndicator.Visibility);
		this.OneWayBind(ViewModel, vm => vm.UpdatesViewVisibility, view => view.UpdatesToggleButtonExpandImage.Visibility);
		this.OneWayBind(ViewModel, vm => vm.UpdateCountVisibility, view => view.UpdateCountTextBlock.Visibility);
		this.OneWayBind(ViewModel, vm => vm.ModUpdatesViewData.TotalUpdates, view => view.UpdateCountTextBlock.Text);

		this.OneWayBind(ViewModel, vm => vm.ModOrderList, view => view.OrdersComboBox.ItemsSource);
		this.Bind(ViewModel, vm => vm.SelectedModOrderIndex, view => view.OrdersComboBox.SelectedIndex);
		this.OneWayBind(ViewModel, vm => vm.IsRenamingOrder, view => view.OrdersComboBox.IsEditable);
		this.OneWayBind(ViewModel, vm => vm.SelectedModOrderName, view => view.OrdersComboBox.Text);

		this.OneWayBind(ViewModel, vm => vm.Profiles, view => view.ProfilesComboBox.ItemsSource);
		this.Bind(ViewModel, vm => vm.SelectedProfileIndex, view => view.ProfilesComboBox.SelectedIndex);

		this.OneWayBind(Services.Mods, vm => vm.AdventureMods, view => view.AdventureModComboBox.ItemsSource);
		this.Bind(ViewModel, vm => vm.SelectedAdventureModIndex, view => view.AdventureModComboBox.SelectedIndex);
		this.OneWayBind(ViewModel, vm => vm.AdventureModBoxVisibility, view => view.AdventureModComboBox.Visibility);
		this.OneWayBind(ViewModel, vm => vm.SelectedAdventureMod, view => view.AdventureModComboBox.Tag);

		this.BindCommand(ViewModel, vm => vm.ToggleUpdatesViewCommand, view => view.UpdateViewToggleButton);

		this.BindCommand(ViewModel, vm => vm.Keys.Save.Command, view => view.SaveButton);
		this.BindCommand(ViewModel, vm => vm.Keys.SaveAs.Command, view => view.SaveAsButton);
		this.BindCommand(ViewModel, vm => vm.Keys.NewOrder.Command, view => view.AddNewOrderButton);
		this.BindCommand(ViewModel, vm => vm.Keys.ExportOrderToGame.Command, view => view.ExportToModSettingsButton);
		this.BindCommand(ViewModel, vm => vm.Keys.ExportOrderToZip.Command, view => view.ExportOrderToArchiveButton);
		this.BindCommand(ViewModel, vm => vm.Keys.ExportOrderToArchiveAs.Command, view => view.ExportOrderToArchiveAsButton);
		this.BindCommand(ViewModel, vm => vm.Keys.Refresh.Command, view => view.RefreshButton);
		this.BindCommand(ViewModel, vm => vm.Keys.OpenModsFolder.Command, view => view.OpenModsFolderButton);
		this.BindCommand(ViewModel, vm => vm.Keys.OpenWorkshopFolder.Command, view => view.OpenWorkshopFolderButton);
		this.BindCommand(ViewModel, vm => vm.Keys.OpenLogsFolder.Command, view => view.OpenExtenderLogsFolderButton);
		this.BindCommand(ViewModel, vm => vm.Keys.LaunchGame.Command, view => view.OpenGameButton);
		this.BindCommand(ViewModel, vm => vm.Keys.OpenDonationLink.Command, view => view.OpenDonationPageButton);
		this.BindCommand(ViewModel, vm => vm.Keys.OpenRepositoryPage.Command, view => view.OpenRepoPageButton);

		this.OneWayBind(ViewModel, vm => vm.LogFolderShortcutButtonVisibility, view => view.OpenExtenderLogsFolderButton.Visibility);
		this.OneWayBind(ViewModel, vm => vm.OpenGameButtonToolTip, view => view.OpenGameButtonToolTipTextBlock.Text);

		this.OneWayBind(ViewModel, vm => vm.SelectedModOrder.FilePath, view => view.OrdersContextMenuOpenMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedModOrder.FilePath, view => view.OrdersContextMenuCopyMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedModOrder, view => view.OrdersContextMenuDeleteMenuItem.CommandParameter);
		//this.BindCommand(ViewModel, vm => vm.CopyPathToClipboardCommand, view => view.OrdersContextMenuCopyMenuItem);
		this.BindCommand(ViewModel, vm => vm.ToggleOrderRenamingCommand, view => view.OrdersContextMenuRenameMenuItem);
		this.BindCommand(ViewModel, vm => vm.DeleteOrderCommand, view => view.OrdersContextMenuDeleteMenuItem);
		var canOpenOrderPath = ViewModel.WhenAnyValue(x => x.SelectedModOrder.FilePath).Select(PathExists);
		canOpenOrderPath.BindTo(this, x => x.OrdersContextMenuOpenMenuItem.IsEnabled);
		canOpenOrderPath.BindTo(this, x => x.OrdersContextMenuCopyMenuItem.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.SelectedProfile.ModSettingsFile, view => view.ExportContextMenuOpenDirectMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedProfile.ModSettingsFile, view => view.ExportContextMenuOpenExplorerMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedProfile.ModSettingsFile, view => view.ExportContextMenuCopyPathMenuItem.CommandParameter);
		//this.BindCommand(ViewModel, vm => vm.CopyPathToClipboardCommand, view => view.ExportContextMenuCopyPathMenuItem);
		this.BindCommand(ViewModel, vm => vm.CopyOrderToClipboardCommand, view => view.ExportContextMenuCopyOrderMenuItem);
		this.BindCommand(ViewModel, vm => vm.ExportOrderAsListCommand, view => view.ExportContextMenuExportListMenuItem);
		var canOpenModSettingsPath = ViewModel.WhenAnyValue(x => x.SelectedProfile.ModSettingsFile).Select(PathExists);
		canOpenModSettingsPath.BindTo(this, x => x.ExportContextMenuOpenDirectMenuItem.IsEnabled);
		canOpenModSettingsPath.BindTo(this, x => x.ExportContextMenuOpenExplorerMenuItem.IsEnabled);
		canOpenModSettingsPath.BindTo(this, x => x.ExportContextMenuCopyPathMenuItem.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.SelectedAdventureMod.FilePath, view => view.AdventureContextMenuOpenMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedAdventureMod.FilePath, view => view.AdventureContextMenuCopyMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedAdventureMod, view => view.AdventureContextMenuModDeveloperMenuItem.DataContext);
		//this.BindCommand(ViewModel, vm => vm.CopyPathToClipboardCommand, view => view.AdventureContextMenuCopyMenuItem);
		var canOpenAdventurePath = ViewModel.WhenAnyValue(x => x.SelectedAdventureMod.FilePath).Select(PathExists);
		canOpenAdventurePath.BindTo(this, x => x.AdventureContextMenuOpenMenuItem.IsEnabled);
		canOpenAdventurePath.BindTo(this, x => x.AdventureContextMenuCopyMenuItem.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.SelectedProfile.FilePath, view => view.ProfilesContextMenuOpenMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.SelectedProfile.FilePath, view => view.ProfilesContextMenuCopyMenuItem.CommandParameter);
		//this.BindCommand(ViewModel, vm => vm.CopyPathToClipboardCommand, view => view.ProfilesContextMenuCopyMenuItem);
		var canOpenProfilePath = ViewModel.WhenAnyValue(x => x.SelectedProfile.FilePath).Select(PathExists);
		canOpenProfilePath.BindTo(this, x => x.ProfilesContextMenuOpenMenuItem.IsEnabled);
		canOpenProfilePath.BindTo(this, x => x.ProfilesContextMenuCopyMenuItem.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.Settings.ExtenderLogDirectory, view => view.OpenExtenderLogsFolderButtonExplorerMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.Settings.GameExecutablePath, view => view.OpenGameButtonExplorerMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.Settings.GameExecutablePath, view => view.OpenGameButtonCopyMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.Settings.WorkshopPath, view => view.OpenWorkshopFolderButtonCopyMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.PathwayData.AppDataModsPath, view => view.OpenModsFolderButtonCopyMenuItem.CommandParameter);

		this.BindCommand(ViewModel, vm => vm.RefreshModUpdatesCommand, view => view.UpdateAllSourcesMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForGitHubModUpdatesCommand, view => view.UpdateGitHubMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForNexusModsUpdatesCommand, view => view.UpdateNexusModsMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForSteamWorkshopUpdatesCommand, view => view.UpdateSteamWorkshopMenuItem);

		this.Bind(ViewModel, vm => vm.Settings.ActionOnGameLaunch, view => view.GameLaunchActionComboBox.SelectedValue);

		this.OneWayBind(ViewModel, vm => vm.UpdatesViewVisibility, view => view.ModUpdaterPanel.Visibility);
		var whenUpdatesViewData = ViewModel.WhenAnyValue(x => x.ModUpdatesViewData);
		whenUpdatesViewData.BindTo(this, x => x.ModUpdaterPanel.ViewModel);
		whenUpdatesViewData.BindTo(this, x => x.ModUpdaterPanel.DataContext);
		//this.OneWayBind(ViewModel, vm => vm.ModUpdatesViewData, view => view.ModUpdaterPanel.ViewModel);

		RegisterKeyBindings();

		this.DeleteFilesView.ViewModel.FileDeletionComplete += (o, e) =>
		{
			DivinityApp.Log($"Deleted {e.TotalFilesDeleted} file(s).");
			if (e.TotalFilesDeleted > 0)
			{
				if (!e.IsDeletingDuplicates)
				{
					var deletedUUIDs = e.DeletedFiles.Where(x => !x.IsWorkshop).Select(x => x.UUID).ToHashSet();
					//var deletedWorkshopUUIDs = e.DeletedFiles.Where(x => x.IsWorkshop).Select(x => x.UUID).ToHashSet();
					ViewModel.RemoveDeletedMods(deletedUUIDs, e.RemoveFromLoadOrder);
				}
				main.Activate();
			}
		};

		FocusManager.SetFocusedElement(this, ModOrderPanel);
	}

	public MainViewControl(MainWindow window, MainWindowViewModel vm)
	{
		InitializeComponent();

		main = window;
		ViewModel = vm;

		DownloadBar.ViewModel = vm.DownloadBar;
	}
}
