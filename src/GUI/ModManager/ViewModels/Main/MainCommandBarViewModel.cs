using DynamicData;
using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Menu;
using ModManager.Util;
using ModManager.Windows;

using System.Collections.ObjectModel;
using System.Reflection;

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

	[Keybinding("Reload All Mods", Key.F5, KeyModifiers.Shift, "Reload mod data without doing a full reload (i.e. reload metadata like the name)", "File")]
	public RxCommandUnit? ReloadModsCommand { get; set; }

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


	[Keybinding("Import Mods...", Key.O, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ImportModCommand { get; set; }

	[Keybinding("Import Nexus Mods Data from Archives...", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? ImportNexusModsIdsCommand { get; set; }

	[Keybinding("Add New Order", Key.N, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? NewOrderCommand { get; set; }

	[Keybinding("Save Order", Key.S, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? SaveCommand { get; set; }

	[Keybinding("Save Order As...", Key.S, KeyModifiers.Control | KeyModifiers.Alt, "", "File")]
	public RxCommandUnit? SaveAsCommand { get; set; }

	[Keybinding("Import Order from Save...", Key.I, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ImportOrderFromSaveCommand { get; set; }

	[Keybinding("Import Order from Save As New Order...", Key.I, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ImportOrderFromSaveAsNewCommand { get; set; }

	[Keybinding("Import Order from File...", Key.O, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ImportOrderFromFileCommand { get; set; }

	[Keybinding("Import Order & Mods from Archive...", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? ImportOrderFromZipFileCommand { get; set; }

	[Keybinding("Export Order to Game", Key.E, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ExportOrderToGameCommand { get; set; }

	[Keybinding("Export Order to Text File...", Key.E, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ExportOrderToListCommand { get; set; }

	[Keybinding("Export Order to Archive (.zip)", Key.R, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ExportOrderToZipCommand { get; set; }

	[Keybinding("Export Order to Archive As...", Key.R, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ExportOrderToArchiveAsCommand { get; set; }

	[Keybinding("Moved Selected Mods to Opposite List", Key.Enter, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? MoveSelectedModsCommand { get; set; }

	[Keybinding("Focus Active Mods List", Key.Left, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? FocusActiveModsCommand { get; set; }

	[Keybinding("Focus Inactive Mods List", Key.Right, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? FocusInactiveModsCommand { get; set; }

	[Keybinding("Go to Other List", Key.Tab, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? SwapListFocusCommand { get; set; }

	[Keybinding("Move to Top of Active List", Key.PageUp, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? MoveToTopCommand { get; set; }

	[Keybinding("Move to Bottom of Active List", Key.PageDown, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? MoveToBottomCommand { get; set; }

	[Keybinding("Toggle Focus Filter for Current List", Key.F, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? ToggleFilterFocusCommand { get; set; }

	[Keybinding("Show File Names for Mods", Key.None, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? ToggleFileNameDisplayCommand { get; set; }

	[Keybinding("Delete Selected Mods...", Key.Delete, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? DeleteSelectedModsCommand { get; set; }

	[Keybinding("Open Preferences", Key.P, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? OpenPreferencesCommand { get; set; }

	[Keybinding("Open Keyboard Shortcuts", Key.K, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? OpenKeybindingsCommand { get; set; }

	[Keybinding("Toggle Light/Dark Mode", Key.L, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? ToggleViewThemeCommand { get; set; } //Key.L, ModifierKeys.Control);

	[Keybinding("Open Game Folder", Key.D2, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenGameFolderCommand { get; set; }

	[Keybinding("Open Saves Folder", Key.D3, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenSavesFolderCommand { get; set; }

	[Keybinding("Open Script Extender Data Folder", Key.D4, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenExtenderDataFolderCommand { get; set; }

	[Keybinding("Download & Extract the Script Extender...", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? DownloadScriptExtenderCommand { get; set; }

	[Keybinding(@"Download nxm:\\ Link...", Key.None, KeyModifiers.None, "Download a NexusMods link for a mod file or a collection", "Download")]
	public RxCommandUnit? DownloadNXMLinkCommand { get; set; }

	[Keybinding("Open Collection Downloader Window", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? OpenCollectionDownloaderWindowCommand { get; set; }

	[Keybinding("Extract All Selected Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractAllSelectedModsCommand { get; set; }

	[Keybinding("Extract Selected Active Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedActiveModsCommand { get; set; }

	[Keybinding("Extract Selected Inactive Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedInactiveModsCommand { get; set; }

	[Keybinding("Extract Active Adventure Mod To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedAdventureCommand { get; set; }

	[Keybinding("Toggle Version Generator Window", Key.G, KeyModifiers.Control, "A tool for mod authors to generate version numbers for a mod's meta.lsx", "Tools")]
	public RxCommandUnit? ToggleVersionGeneratorWindowCommand { get; set; }

	[Keybinding("Speak Active Order", Key.Home, KeyModifiers.Control, "", "Tools")]
	public RxCommandUnit? SpeakActiveModOrderCommand { get; set; }

	[Keybinding("Stop Speaking", Key.Home, KeyModifiers.Control | KeyModifiers.Shift, "", "Tools")]
	public RxCommandUnit? StopSpeakingCommand { get; set; }

	[Keybinding("Check for Updates...", Key.F7, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? CheckForUpdatesCommand { get; set; }

	[Keybinding("Donate a Coffee...", Key.F10, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenDonationLinkCommand { get; set; }

	[Keybinding("About", Key.F1, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenAboutWindowCommand { get; set; }

	[Keybinding("Open Repository Page...", Key.F11, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenRepositoryPageCommand { get; set; }

	private readonly ObservableCollectionExtended<IMenuEntry> _menuEntries = [];

	private readonly ReadOnlyObservableCollection<IMenuEntry> _uiMenuEntries;
	public ReadOnlyObservableCollection<IMenuEntry> MenuEntries => _uiMenuEntries;

	[Reactive] public IModOrderViewModel? ModOrder { get; set; }

	public MainCommandBarViewModel()
	{
		_menuEntries.ToObservableChangeSet().Bind(out _uiMenuEntries).Subscribe();
	}

	public MainCommandBarViewModel(MainWindowViewModel main, ModOrderViewModel modOrder) : this()
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

		var keybindings = this.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetCustomAttribute<KeybindingAttribute>());

		//MenuEntry.FromKeybinding(ImportNexusModsIdsCommand, nameof(ImportNexusModsIdsCommand), keybindings),
		_menuEntries.AddRange([
			new MenuEntry("_File"){
				Children = [
					MenuEntry.FromKeybinding(ImportModCommand, nameof(ImportModCommand), keybindings),
					MenuEntry.FromKeybinding(ImportNexusModsIdsCommand, nameof(ImportNexusModsIdsCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(SaveOrderCommand, nameof(SaveOrderCommand), keybindings),
					MenuEntry.FromKeybinding(SaveOrderAsCommand, nameof(SaveOrderAsCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(AddNewOrderCommand, nameof(AddNewOrderCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ImportOrderFromSaveCommand, nameof(ImportOrderFromSaveCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromSaveAsNewCommand, nameof(ImportOrderFromSaveAsNewCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromFileCommand, nameof(ImportOrderFromFileCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromZipFileCommand, nameof(ImportOrderFromZipFileCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ExportOrderCommand, nameof(ExportOrderCommand), keybindings),
					MenuEntry.FromKeybinding(ExportOrderToListCommand, nameof(ExportOrderToListCommand), keybindings),
					MenuEntry.FromKeybinding(ExportOrderToZipCommand, nameof(ExportOrderToZipCommand), keybindings),
					MenuEntry.FromKeybinding(ExportOrderToArchiveAsCommand, nameof(ExportOrderToArchiveAsCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ReloadModsCommand, nameof(ReloadModsCommand), keybindings),
					MenuEntry.FromKeybinding(RefreshModUpdatesCommand, nameof(RefreshModUpdatesCommand), keybindings),
				]},
			new MenuEntry("_Edit"){
				Children = [
					MenuEntry.FromKeybinding(MoveSelectedModsCommand, nameof(MoveSelectedModsCommand), keybindings),
					MenuEntry.FromKeybinding(FocusActiveModsCommand, nameof(FocusActiveModsCommand), keybindings),
					MenuEntry.FromKeybinding(FocusInactiveModsCommand, nameof(FocusInactiveModsCommand), keybindings),
					MenuEntry.FromKeybinding(SwapListFocusCommand, nameof(SwapListFocusCommand), keybindings),
					MenuEntry.FromKeybinding(MoveToTopCommand, nameof(MoveToTopCommand), keybindings),
					MenuEntry.FromKeybinding(MoveToBottomCommand, nameof(MoveToBottomCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ToggleFilterFocusCommand, nameof(ToggleFilterFocusCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleFileNameDisplayCommand, nameof(ToggleFileNameDisplayCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(DeleteSelectedModsCommand, nameof(DeleteSelectedModsCommand), keybindings),
				]},
			new MenuEntry("_Settings"){
				Children = [
					MenuEntry.FromKeybinding(ToggleSettingsWindowCommand, nameof(ToggleSettingsWindowCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleKeybindingsCommand, nameof(ToggleKeybindingsCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleThemeModeCommand, nameof(ToggleThemeModeCommand), keybindings),
				]},
			new MenuEntry("_View"){
				Children = [
					MenuEntry.FromKeybinding(ToggleUpdatesViewCommand, nameof(ToggleUpdatesViewCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleVersionGeneratorWindowCommand, nameof(ToggleVersionGeneratorWindowCommand), keybindings),
					MenuEntry.FromKeybinding(TogglePakFileExplorerWindowCommand, nameof(TogglePakFileExplorerWindowCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleStatsValidatorWindowCommand, nameof(ToggleStatsValidatorWindowCommand), keybindings),
				]},
			new MenuEntry("_Go"){
				Children = [
					MenuEntry.FromKeybinding(LaunchGameCommand, nameof(LaunchGameCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(OpenNexusModsCommand, nameof(OpenNexusModsCommand), keybindings),
					MenuEntry.FromKeybinding(OpenSteamPageCommand, nameof(OpenSteamPageCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(OpenModsFolderCommand, nameof(OpenModsFolderCommand), keybindings),
					MenuEntry.FromKeybinding(OpenGameFolderCommand, nameof(OpenGameFolderCommand), keybindings),
					MenuEntry.FromKeybinding(OpenSavesFolderCommand, nameof(OpenSavesFolderCommand), keybindings),
					MenuEntry.FromKeybinding(OpenExtenderDataFolderCommand, nameof(OpenExtenderDataFolderCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(OpenGitHubRepoCommand, nameof(OpenGitHubRepoCommand), keybindings),
				]},
			new MenuEntry("_Download"){
				Children = [
					MenuEntry.FromKeybinding(DownloadScriptExtenderCommand, nameof(DownloadScriptExtenderCommand), keybindings),
					MenuEntry.FromKeybinding(DownloadNXMLinkCommand, nameof(DownloadNXMLinkCommand), keybindings),
					MenuEntry.FromKeybinding(OpenCollectionDownloaderWindowCommand, nameof(OpenCollectionDownloaderWindowCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(CheckAllModUpdatesCommand, nameof(CheckAllModUpdatesCommand), keybindings, [
						MenuEntry.FromKeybinding(CheckAllModUpdatesCommand, nameof(CheckAllModUpdatesCommand), keybindings),
						new MenuSeparator(),
						MenuEntry.FromKeybinding(CheckForGitHubModUpdatesCommand, nameof(CheckForGitHubModUpdatesCommand), keybindings),
						MenuEntry.FromKeybinding(CheckForNexusModsUpdatesCommand, nameof(CheckForNexusModsUpdatesCommand), keybindings),
						MenuEntry.FromKeybinding(CheckForModioUpdatesCommand, nameof(CheckForModioUpdatesCommand), keybindings)
					]),
				]},
			new MenuEntry("_Tools"){
				Children = [
					new MenuEntry("Extract..."){
					Children = [
						MenuEntry.FromKeybinding(ExtractAllSelectedModsCommand, nameof(ExtractAllSelectedModsCommand), keybindings),
						new MenuSeparator(),
						MenuEntry.FromKeybinding(ExtractSelectedActiveModsCommand, nameof(ExtractSelectedActiveModsCommand), keybindings),
						MenuEntry.FromKeybinding(ExtractSelectedInactiveModsCommand, nameof(ExtractSelectedInactiveModsCommand), keybindings),
						new MenuSeparator(),
						MenuEntry.FromKeybinding(ExtractSelectedAdventureCommand, nameof(ExtractSelectedAdventureCommand), keybindings),
					]},
					new MenuSeparator(),
					new MenuEntry("Speak..."){
					Children = [
						MenuEntry.FromKeybinding(SpeakActiveModOrderCommand, nameof(SpeakActiveModOrderCommand), keybindings),
						MenuEntry.FromKeybinding(StopSpeakingCommand, nameof(StopSpeakingCommand), keybindings),
					]},
				]},
			new MenuEntry("_Help"){
				Children = [
					MenuEntry.FromKeybinding(OpenAboutWindowCommand, nameof(OpenAboutWindowCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(CheckForUpdatesCommand, nameof(CheckForUpdatesCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(OpenDonationLinkCommand, nameof(OpenDonationLinkCommand), keybindings),
				]},
		]);

		this.RegisterKeybindings();
	}
}

public class DesignMainCommandBarViewModel : MainCommandBarViewModel
{
	public DesignMainCommandBarViewModel() : base()
	{
		ModOrder = new DesignModOrderViewModel();
	}
}