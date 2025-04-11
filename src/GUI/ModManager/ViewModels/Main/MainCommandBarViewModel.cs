using DynamicData;
using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Menu;
using ModManager.Util;
using ModManager.Windows;

using System.Collections.ObjectModel;

namespace ModManager.ViewModels.Main;

[Keybindings]
public partial class MainCommandBarViewModel : ReactiveObject
{
	[Keybinding("Add New Order", Key.M)]
	public RxCommandUnit? AddNewOrderCommand { get; set; }

	[Keybinding("Check For App Updates", Key.U)]
	public RxCommandUnit? CheckForAppUpdatesCommand { get; set; }

	public RxCommandUnit? CheckAllModUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForGitHubModUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForNexusModsUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForModioUpdatesCommand { get; set; }

	[Keybinding("Export Order to Archive As...", Key.R, KeyModifiers.Control | KeyModifiers.Shift, "Export all active mods to an archive file of a chosen type", "File")]
	public RxCommandUnit? ExportModsToZipAsCommand { get; set; }

	[Keybinding("Export Order to Archive (.zip)", Key.R, KeyModifiers.Control, "Export all active mods to zip", "File")]
	public RxCommandUnit? ExportModsToZipCommand { get; set; }

	[Keybinding("Export Order to Game", Key.E, KeyModifiers.Control, "Export order to modsettings.lsx", "File")]
	public ReactiveCommand<Unit,bool>? ExportOrderCommand { get; set; }

	[Keybinding("Launch Game", Key.G, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? LaunchGameCommand { get; set; }

	[Keybinding("Donate a Coffee...", Key.F10, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenDonationPageCommand { get; set; }

	[Keybinding("Open Repository Page...", Key.F11, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenGitHubRepoCommand { get; set; }

	[Keybinding("Open Mods Folder", Key.D1, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenModsFolderCommand { get; set; }

	[Keybinding("Open Nexus Mods Website", Key.D3, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenNexusModsCommand { get; set; }

	[Keybinding("Open Steam Store Page", Key.D4, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenSteamPageCommand { get; set; }

	[Keybinding("Refresh Mods", Key.F5, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RefreshCommand { get; set; }

	[Keybinding("Refresh Mod Updates", Key.F6, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RefreshModUpdatesCommand { get; set; }

	[Keybinding("Rename Save", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RenameSaveCommand { get; set; }

	[Keybinding("Save Order", Key.S, KeyModifiers.Control, "", "File")]
	public ReactiveCommand<Unit, bool>? SaveOrderCommand { get; set; }

	[Keybinding("Save Order As...", Key.S, KeyModifiers.Control | KeyModifiers.Alt, "", "File")]
	public ReactiveCommand<Unit, bool>? SaveOrderAsCommand { get; set; }

	[Keybinding("Save Settings", Key.S, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? SaveSettingsSilentlyCommand { get; set; }

	[Keybinding("Toggle Updates View", Key.U, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxCommandUnit? ToggleUpdatesViewCommand { get; set; }

	[Keybinding("Toggle Pak File Explorer Window", Key.P, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxCommandUnit? TogglePakFileExplorerWindowCommand { get; set; }

	[Keybinding("Toggle Stats Validator Window", Key.OemBackslash, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxCommandUnit? ToggleStatsValidatorWindowCommand { get; set; }

	[Keybinding("Toggle Settings Window", Key.OemComma, KeyModifiers.Control)]
	public ReactiveCommand<Unit,bool>? ToggleSettingsWindowCommand { get; set; }

	[Keybinding("Toggle Keybindings Window", Key.OemComma, KeyModifiers.Control | KeyModifiers.Alt)]
	public RxCommandUnit? ToggleKeybindingsCommand { get; set; }

	[Keybinding("Toggle Dark/Light Mode", Key.OemComma, KeyModifiers.Control | KeyModifiers.Alt)]
	public RxCommandUnit? ToggleThemeModeCommand { get; set; }

	private readonly ObservableCollectionExtended<IMenuEntry> _menuEntries = [];

	private readonly ReadOnlyObservableCollection<IMenuEntry> _uiMenuEntries;
	public ReadOnlyObservableCollection<IMenuEntry> MenuEntries => _uiMenuEntries;

	[Reactive] public IModOrderViewModel? ModOrder { get; set; }

	public void CreateCommands(MainWindowViewModel main, ModOrderViewModel modOrder)
	{
		var canExecuteCommands = main.WhenAnyValue(x => x.IsLocked, b => !b);

		var isModOrderView = main.WhenAnyValue(x => x.Router.CurrentViewModel, vm => vm == modOrder);
		var canExecuteModOrderCommands = canExecuteCommands.CombineLatest(isModOrderView).Select(x => x.First && x.Second);

		AddNewOrderCommand = ReactiveCommand.Create(() => modOrder.AddNewModOrder(), canExecuteModOrderCommands);

		CheckForAppUpdatesCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Commands.ShowAlert("Checking for updates...", AlertType.Info, 30);
			//main.CheckForUpdates(true);
			main.SaveSettings();
		}, canExecuteCommands);

		var anyDownloadAllowed = main.WhenAnyValue(x => x.GitHubModSupportEnabled, x => x.NexusModsSupportEnabled, x => x.ModioSupportEnabled).Select(x => x.Item1 || x.Item2 || x.Item3);

		CheckAllModUpdatesCommand = ReactiveCommand.Create(main.RefreshAllModUpdatesBackground, anyDownloadAllowed.AllTrue(canExecuteCommands));
		CheckForGitHubModUpdatesCommand = ReactiveCommand.Create(main.RefreshGitHubModsUpdatesBackground, main.WhenAnyValue(x => x.GitHubModSupportEnabled).AllTrue(canExecuteCommands));
		CheckForNexusModsUpdatesCommand = ReactiveCommand.Create(main.RefreshNexusModsUpdatesBackground, main.WhenAnyValue(x => x.NexusModsSupportEnabled).AllTrue(canExecuteCommands));
		CheckForModioUpdatesCommand = ReactiveCommand.Create(main.RefreshModioUpdatesBackground, main.WhenAnyValue(x => x.ModioSupportEnabled).AllTrue(canExecuteCommands));

		ExportOrderCommand = ReactiveCommand.CreateFromTask(modOrder.ExportLoadOrderAsync, canExecuteCommands);
		ExportModsToZipCommand = ReactiveCommand.CreateFromTask(main.ExportLoadOrderToArchiveAsync, canExecuteCommands);
		ExportModsToZipAsCommand = ReactiveCommand.CreateFromTask(main.ExportLoadOrderToArchiveAsAsync, canExecuteCommands);

		LaunchGameCommand = ReactiveCommand.Create(main.LaunchGame, main.WhenAnyValue(x => x.CanLaunchGame).AllTrue(canExecuteCommands));

		OpenGitHubRepoCommand = ReactiveCommand.Create(() => FileUtils.TryOpenPath(DivinityApp.URL_REPO), canExecuteCommands);
		OpenDonationPageCommand = ReactiveCommand.Create(() => FileUtils.TryOpenPath(DivinityApp.URL_DONATION), canExecuteCommands);
		OpenModsFolderCommand = ReactiveCommand.Create(() => FileUtils.TryOpenPath(main.PathwayData.AppDataModsPath), canExecuteCommands);
		OpenNexusModsCommand = ReactiveCommand.Create(() => FileUtils.TryOpenPath(DivinityApp.URL_NEXUSMODS), canExecuteCommands);
		OpenSteamPageCommand = ReactiveCommand.Create(() => FileUtils.TryOpenPath(DivinityApp.URL_STEAM), canExecuteCommands);

		var canRefreshModUpdates = canExecuteCommands.CombineLatest(main.WhenAnyValue(x => x.IsRefreshingModUpdates, x => x.AppSettingsLoaded))
			.Select(x => x.First && !x.Second.Item1 && x.Second.Item2);

		RefreshCommand = ReactiveCommand.Create(main.RefreshStart, canExecuteCommands);
		RefreshModUpdatesCommand = ReactiveCommand.Create(main.RefreshModUpdates, canRefreshModUpdates);

		RenameSaveCommand = ReactiveCommand.CreateFromTask(main.RenameSaveAsync, canExecuteCommands);

		SaveOrderCommand = ReactiveCommand.CreateFromTask(modOrder.SaveLoadOrderAsync, canExecuteCommands);
		SaveOrderAsCommand = ReactiveCommand.CreateFromTask(modOrder.SaveLoadOrderAs, canExecuteCommands);
		SaveSettingsSilentlyCommand = ReactiveCommand.Create(main.SaveSettings, canExecuteCommands);

		var canToggleUpdatesView = canExecuteCommands.CombineLatest(main.WhenAnyValue(x => x.ModUpdatesAvailable)).Select(x => x.First && x.Second);

		ToggleUpdatesViewCommand = ReactiveCommand.Create(() =>
		{
			if (main.Router.GetCurrentViewModel() != ViewModelLocator.ModUpdates)
			{
				main.Views.SwitchToModUpdates();
			}
			else
			{
				main.Views.SwitchToModOrderView();
			}
		}, canToggleUpdatesView);

		TogglePakFileExplorerWindowCommand = ReactiveCommand.Create(() =>
		{
			var window = AppServices.Get<PakFileExplorerWindow>()!;
			if (window.IsVisible)
			{
				window.Hide();
			}
			else
			{
				window.Show();
			}
		});

		ToggleStatsValidatorWindowCommand = ReactiveCommand.Create(() =>
		{
			var window = AppServices.Get<StatsValidatorWindow>()!;
			if (window.IsVisible)
			{
				window.Hide();
			}
			else
			{
				window.Show();
			}
		});

		ToggleSettingsWindowCommand = ReactiveCommand.Create(() =>
		{
			var window = AppServices.Get<SettingsWindow>()!;
			if (window.IsVisible)
			{
				window.Hide();
				return false;
			}
			else
			{
				window.Show();
				return true;
			}
		});

		ToggleKeybindingsCommand = ReactiveCommand.Create(() =>
		{
			ToggleSettingsWindowCommand.Execute().Subscribe(isVisible =>
			{
				if (isVisible)
				{
					var vm = AppServices.Get<SettingsWindowViewModel>();
					vm.SelectedTabIndex = SettingsWindowTab.Keybindings;
				}
			});
		});

		ToggleThemeModeCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Settings.ManagerSettings.DarkThemeEnabled = !AppServices.Settings.ManagerSettings.DarkThemeEnabled;
		});

		_menuEntries.AddRange([
			new MenuEntry("_File"){
				Children = [
					new MenuEntry("Import Mods..."),
					new MenuEntry("Import Nexus Mods Data from Archives..."),
					new MenuSeparator(),
					new MenuEntry("Save Order", SaveOrderCommand),
					new MenuEntry("Save Order As...", SaveOrderAsCommand),
					new MenuSeparator(),
					new MenuEntry("Add New Order", AddNewOrderCommand),
					new MenuSeparator(),
					new MenuEntry("Import Order from Save..."),
					new MenuEntry("Import Order from Save As New Order..."),
					new MenuEntry("Import Order from File..."),
					new MenuEntry("Import Order & Mods from Archive..."),
					new MenuSeparator(),
					new MenuEntry("Export Order to Game"),
					new MenuEntry("Export Order to Text File..."),
					new MenuEntry("Export Order to Archive (.zip)"),
					new MenuEntry("Export Order to Archive As..."),
					new MenuSeparator(),
					new MenuEntry("Reload All Mods"),
					new MenuEntry("Refresh Mod Updates"),
				]},
			new MenuEntry("_Edit"){
				Children = [
					new MenuEntry("Moved Selected Mods to Opposite List"),
					new MenuEntry("Focus Active Mods List"),
					new MenuEntry("Focus Inactive Mods List"),
					new MenuEntry("Go to Other List"),
					new MenuEntry("Move to Top of Active List"),
					new MenuEntry("Move to Bottom of Active List"),
					new MenuSeparator(),
					new MenuEntry("Toggle Focus Filter for Current List"),
					new MenuEntry("Show File Names for Mods"),
					new MenuSeparator(),
					new MenuEntry("Delete Selected Mods..."),
				]},
			new MenuEntry("_Settings"){
				Children = [
					new MenuEntry("Toggle Settings Window", ToggleSettingsWindowCommand),
					new MenuEntry("Toggle Keybindings Window", ToggleKeybindingsCommand),
					new MenuEntry("Toggle Dark/Light Mode", ToggleThemeModeCommand),
				]},
			new MenuEntry("_View"){
				Children = [
					new MenuEntry("Toggle Updates View", ToggleUpdatesViewCommand),
					new MenuEntry("Toggle Version Generator Window"),
					new MenuEntry("Toggle Pak File Explorer Window", TogglePakFileExplorerWindowCommand),
					new MenuEntry("Toggle Stats Validator Window", ToggleStatsValidatorWindowCommand),
				]},
			new MenuEntry("_Go"){
				Children = [
					new MenuEntry("Launch Game", LaunchGameCommand),
					new MenuSeparator(),
					new MenuEntry("Open Nexus Mods Page...", OpenNexusModsCommand),
					new MenuEntry("Open Steam Page...", OpenSteamPageCommand),
					new MenuSeparator(),
					new MenuEntry("Open Mods Folder", OpenModsFolderCommand),
					new MenuEntry("Open Game Folder"),
					new MenuEntry("Open Saves Folder"),
					new MenuEntry("Open Extender Data Folder"),
					new MenuSeparator(),
					new MenuEntry("Open Repository Page...", OpenGitHubRepoCommand),
				]},
			new MenuEntry("_Download"){
				Children = [
					new MenuEntry("Download & Extract the Script Extender..."),
					new MenuEntry(@"Download nxm:\\ Link..."),
					new MenuEntry(@"Open Collection Downloader Window"),
					new MenuSeparator(),
					new MenuEntry(@"Check For Mod Updates...", CheckAllModUpdatesCommand){
					Children = [
						new MenuEntry("All", CheckAllModUpdatesCommand),
						new MenuSeparator(),
						new MenuEntry("GitHub", CheckForGitHubModUpdatesCommand),
						new MenuEntry("Nexus Mods", CheckForNexusModsUpdatesCommand),
						new MenuEntry("Mod.io", CheckForModioUpdatesCommand),
					]},
				]},
			new MenuEntry("_Tools"){
				Children = [
					new MenuEntry("Extract Selected Mods To..."){
					Children = [
						new MenuEntry("All Selected Mods"),
						new MenuSeparator(),
						new MenuEntry("Selected Active Mods"),
						new MenuEntry("Selected Inactive Mods"),
					]},
					new MenuSeparator(),
					new MenuEntry(@"Speak Active Order"),
				]},
			new MenuEntry("_Help"){
				Children = [
					new MenuEntry("About"),
					new MenuSeparator(),
					new MenuEntry("Check for Updates..."),
					new MenuSeparator(),
					new MenuEntry("Donate a Coffee..."),
				]},
		]);

		this.RegisterKeybindings();
	}

	public MainCommandBarViewModel()
	{
		_menuEntries.ToObservableChangeSet().Bind(out _uiMenuEntries).Subscribe();
	}
}

public class DesignMainCommandBarViewModel : MainCommandBarViewModel
{
	public DesignMainCommandBarViewModel() : base()
	{
		ModOrder = new DesignModOrderViewModel();
	}
}