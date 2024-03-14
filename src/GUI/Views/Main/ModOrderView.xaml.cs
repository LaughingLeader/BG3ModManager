using DivinityModManager.Util;
using DivinityModManager.ViewModels.Main;

using ReactiveUI;

using System.Data;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DivinityModManager.Views.Main;

public partial class ModOrderView : ReactiveUserControl<ModOrderViewModel>
{
	private static bool PathExists(string path) => !String.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path));

	public ModOrderView()
    {
        InitializeComponent();

		this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.ModLayout.ViewModel);

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, vm => vm.ModOrderList, view => view.OrdersComboBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedModOrderIndex, view => view.OrdersComboBox.SelectedIndex);
			this.OneWayBind(ViewModel, vm => vm.IsRenamingOrder, view => view.OrdersComboBox.IsEditable);
			this.OneWayBind(ViewModel, vm => vm.SelectedModOrderName, view => view.OrdersComboBox.Text);

			this.OneWayBind(ViewModel, vm => vm.Profiles, view => view.ProfilesComboBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedProfileIndex, view => view.ProfilesComboBox.SelectedIndex);

			Services.Mods.WhenAnyValue(x => x.AdventureMods).BindTo(this, x => x.AdventureModComboBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedAdventureModIndex, view => view.AdventureModComboBox.SelectedIndex);
			this.OneWayBind(ViewModel, vm => vm.AdventureModBoxVisibility, view => view.AdventureModComboBox.Visibility);
			this.OneWayBind(ViewModel, vm => vm.SelectedAdventureMod, view => view.AdventureModComboBox.Tag);

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

			var settings = Services.Settings.ManagerSettings;

			settings.WhenAnyValue(x => x.ExtenderLogDirectory).BindTo(this, x => x.OpenExtenderLogsFolderButtonExplorerMenuItem.CommandParameter);
			settings.WhenAnyValue(x => x.GameExecutablePath).BindTo(this, x => x.OpenGameButtonExplorerMenuItem.CommandParameter);
			settings.WhenAnyValue(x => x.GameExecutablePath).BindTo(this, x => x.OpenGameButtonCopyMenuItem.CommandParameter);
			settings.WhenAnyValue(x => x.WorkshopPath).BindTo(this, x => x.OpenWorkshopFolderButtonCopyMenuItem.CommandParameter);
			Services.Pathways.Data.WhenAnyValue(x => x.AppDataModsPath).BindTo(this, x => x.OpenModsFolderButtonCopyMenuItem.CommandParameter);

			settings.WhenAnyValue(x => x.ActionOnGameLaunch).BindTo(this, x => x.GameLaunchActionComboBox.SelectedValue);

			FocusManager.SetFocusedElement(this, ModOrderPanel);
		});
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
						DivinityApp.ShowAlert($"Renamed load order name/path to '{ViewModel.SelectedModOrder.FilePath}'", AlertType.Success, 20);
					}
				}
			});
		}
	}

	private void OrderComboBox_OnUserClick(object sender, MouseButtonEventArgs e)
	{
		RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(200), () =>
		{
			var settings = Services.Settings.ManagerSettings;
			if (settings.LastOrder != ViewModel.SelectedModOrder.Name)
			{
				settings.LastOrder = ViewModel.SelectedModOrder.Name;
				settings.Save(out _);
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
}
