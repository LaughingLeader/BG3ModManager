using DynamicData;
using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Menu;
using ModManager.Models.Mod;
using ModManager.Services;
using ModManager.Util;
using ModManager.ViewModels.Mods;
using ModManager.Views.Main;
using ModManager.Views.Mods;
using ModManager.Windows;

using System.Collections.ObjectModel;
using System.Reflection;

namespace ModManager.ViewModels.Main;

[Keybindings]
public partial class MainCommandBarViewModel : ReactiveObject
{
	[Keybinding("Add New Order", Key.M)]
	public RxCommandUnit? AddNewOrderCommand { get; private set; }

	[Keybinding("Check For App Updates", Key.U)]
	public RxCommandUnit? CheckForAppUpdatesCommand { get; private set; }

	[Keybinding("Check for All Mod Updates", Key.None, KeyModifiers.None, "Check all sources of mod updates (GitHub, Nexus, etc.)", "Download")]
	public RxCommandUnit? CheckAllModUpdatesCommand { get; private set; }

	[Keybinding("Check for Github Mod Updates", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? CheckForGitHubModUpdatesCommand { get; private set; }

	[Keybinding("Check for Nexus Mod Updates", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? CheckForNexusModsUpdatesCommand { get; private set; }

	[Keybinding("Check for Mod.io Updates", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? CheckForModioUpdatesCommand { get; private set; }

	[Keybinding("Export Order to Game", Key.E, KeyModifiers.Control, "Export order to modsettings.lsx", "File")]
	public RxBoolCommandUnit? ExportOrderCommand { get; private set; }

	[Keybinding("Export Order to Text File...", Key.E, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ExportOrderToTextFileCommand { get; private set; }

	[Keybinding("Export Order to Archive (.zip)", Key.None, KeyModifiers.None, "Export all active mods to a zip file", "File")]
	public RxCommandUnit? ExportOrderToZipCommand { get; private set; }

	[Keybinding("Export Order to Archive As...", Key.None, KeyModifiers.None, "Export all active mods to an archive file of a chosen type", "File")]
	public RxCommandUnit? ExportOrderToArchiveAsCommand { get; private set; }

	[Keybinding("Rename Order", Key.R, KeyModifiers.Control | KeyModifiers.Shift, "Rename current load order", "File")]
	public RxCommandUnit? RenameOrderCommand { get; private set; }

	[Keybinding("Launch Game", Key.G, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? LaunchGameCommand { get; private set; }

	[Keybinding("Donate a Coffee...", Key.F10, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenDonationPageCommand { get; private set; }

	[Keybinding("Open Repository Page...", Key.F11, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenGitHubRepoCommand { get; private set; }

	[Keybinding("Open Mods Folder", Key.D1, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenModsFolderCommand { get; private set; }

	[Keybinding("Open Logs Folder", Key.D2, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenLogsFolderCommand { get; private set; }

	[Keybinding("Open Nexus Mods Website", Key.D3, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenNexusModsCommand { get; private set; }

	[Keybinding("Open Steam Store Page", Key.D4, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenSteamPageCommand { get; private set; }

	[Keybinding("Refresh Mods", Key.F5, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RefreshCommand { get; private set; }

	[Keybinding("Reload All Mods", Key.F5, KeyModifiers.Shift, "Reload mod data without doing a full reload (i.e. reload metadata like the name)", "File")]
	public RxCommandUnit? ReloadModsCommand { get; private set; }

	[Keybinding("Refresh Mod Updates", Key.F6, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RefreshModUpdatesCommand { get; private set; }

	[Keybinding("Rename Save", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? RenameSaveCommand { get; private set; }

	[Keybinding("Save Order", Key.S, KeyModifiers.Control, "", "File")]
	public RxBoolCommandUnit? SaveOrderCommand { get; private set; }

	[Keybinding("Save Order As...", Key.S, KeyModifiers.Control | KeyModifiers.Alt, "", "File")]
	public RxBoolCommandUnit? SaveOrderAsCommand { get; private set; }

	[Keybinding("Save Settings", Key.S, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? SaveSettingsSilentlyCommand { get; private set; }

	[Keybinding("Toggle Updates View", Key.U, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxCommandUnit? ToggleUpdatesViewCommand { get; private set; }

	[Keybinding("Toggle Pak File Explorer Window", Key.P, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxBoolCommandUnit? TogglePakFileExplorerWindowCommand { get; private set; }

	[Keybinding("Toggle Stats Validator Window", Key.OemBackslash, KeyModifiers.Control | KeyModifiers.Alt, "", "View")]
	public RxBoolCommandUnit? ToggleStatsValidatorWindowCommand { get; private set; }

	[Keybinding("Toggle Settings Window", Key.OemComma, KeyModifiers.Control)]
	public RxBoolCommandUnit? ToggleSettingsWindowCommand { get; private set; }

	[Keybinding("Toggle Keybindings Window", Key.OemComma, KeyModifiers.Control | KeyModifiers.Alt)]
	public RxBoolCommandUnit? ToggleKeybindingsCommand { get; private set; }

	[Keybinding("Toggle Version Generator Window", Key.G, KeyModifiers.Control, "A tool for mod authors to generate version numbers for a mod's meta.lsx", "Tools")]
	public RxBoolCommandUnit? ToggleVersionGeneratorWindowCommand { get; private set; }

	[Keybinding("About", Key.F1, KeyModifiers.None, "", "Help")]
	public RxBoolCommandUnit? ToggleAboutWindowCommand { get; private set; }

	[Keybinding("Toggle Dark/Light Mode", Key.OemComma, KeyModifiers.Control | KeyModifiers.Alt)]
	public RxCommandUnit? ToggleThemeModeCommand { get; private set; }


	[Keybinding("Import Mods...", Key.O, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ImportModCommand { get; private set; }

	[Keybinding("Import Nexus Mods Data from Archives...", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? ImportNexusModsIdsCommand { get; private set; }

	[Keybinding("Add New Order", Key.N, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? NewOrderCommand { get; private set; }

	[Keybinding("Save Order", Key.S, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? SaveCommand { get; private set; }

	[Keybinding("Save Order As...", Key.S, KeyModifiers.Control | KeyModifiers.Alt, "", "File")]
	public RxCommandUnit? SaveAsCommand { get; private set; }

	[Keybinding("Import Order from Save...", Key.I, KeyModifiers.Control, "", "File")]
	public RxCommandUnit? ImportOrderFromSaveCommand { get; private set; }

	[Keybinding("Import Order from Save As New Order...", Key.I, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ImportOrderFromSaveAsNewCommand { get; private set; }

	[Keybinding("Import Order from File...", Key.O, KeyModifiers.Control | KeyModifiers.Shift, "", "File")]
	public RxCommandUnit? ImportOrderFromFileCommand { get; private set; }

	[Keybinding("Import Order & Mods from Archive...", Key.None, KeyModifiers.None, "", "File")]
	public RxCommandUnit? ImportOrderFromZipFileCommand { get; private set; }

	[Keybinding("Moved Selected Mods to Opposite List", Key.Enter, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? MoveSelectedModsCommand { get; private set; }

	[Keybinding("Focus Active Mods List", Key.Left, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? FocusActiveModsCommand { get; private set; }

	[Keybinding("Focus Inactive Mods List", Key.Right, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? FocusInactiveModsCommand { get; private set; }

	[Keybinding("Go to Other List", Key.Tab, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? SwapListFocusCommand { get; private set; }

	[Keybinding("Move to Top of Active List", Key.PageUp, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? MoveToTopCommand { get; private set; }

	[Keybinding("Move to Bottom of Active List", Key.PageDown, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? MoveToBottomCommand { get; private set; }

	[Keybinding("Toggle Focus Filter for Current List", Key.F, KeyModifiers.Control, "", "Edit")]
	public RxCommandUnit? ToggleFilterFocusCommand { get; private set; }

	[Keybinding("Show File Names for Mods", Key.None, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? ToggleFileNameDisplayCommand { get; private set; }

	[Keybinding("Delete Selected Mods...", Key.Delete, KeyModifiers.None, "", "Edit")]
	public RxCommandUnit? DeleteSelectedModsCommand { get; private set; }

	[Keybinding("Open Preferences", Key.P, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? OpenPreferencesCommand { get; private set; }

	[Keybinding("Open Keyboard Shortcuts", Key.K, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? OpenKeybindingsCommand { get; private set; }

	[Keybinding("Toggle Light/Dark Mode", Key.L, KeyModifiers.Control, "", "Settings")]
	public RxCommandUnit? ToggleViewThemeCommand { get; private set; } //Key.L, ModifierKeys.Control);

	[Keybinding("Open Game Folder", Key.D2, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenGameFolderCommand { get; private set; }

	[Keybinding("Open Saves Folder", Key.D3, KeyModifiers.Control, "", "Go")]
	public RxBoolCommandUnit? OpenSavesFolderCommand { get; private set; }

	[Keybinding("Open Script Extender Data Folder", Key.D4, KeyModifiers.Control, "", "Go")]
	public RxCommandUnit? OpenExtenderDataFolderCommand { get; private set; }

	[Keybinding("Download & Extract the Script Extender...", Key.None, KeyModifiers.None, "", "Download")]
	public RxCommandUnit? DownloadScriptExtenderCommand { get; private set; }

	[Keybinding(@"Download nxm:\\ Link...", Key.None, KeyModifiers.None, "Download a NexusMods link for a mod file or a collection", "Download")]
	public RxBoolCommandUnit? ToggleNXMLinkDownloaderCommand { get; private set; }

	[Keybinding("Open Collection Downloader Window", Key.None, KeyModifiers.None, "", "Download")]
	public RxBoolCommandUnit? ToggleCollectionDownloaderWindowCommand { get; private set; }

	[Keybinding("Extract All Selected Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractAllSelectedModsCommand { get; private set; }

	[Keybinding("Extract Selected Active Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedActiveModsCommand { get; private set; }

	[Keybinding("Extract Selected Inactive Mods To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedInactiveModsCommand { get; private set; }

	[Keybinding("Extract Active Adventure Mod To...", Key.None, KeyModifiers.None, "", "Tools")]
	public RxCommandUnit? ExtractSelectedAdventureCommand { get; private set; }

	[Keybinding("Speak Active Order", Key.Home, KeyModifiers.Control, "", "Tools")]
	public RxCommandUnit? SpeakActiveModOrderCommand { get; private set; }

	[Keybinding("Stop Speaking", Key.Home, KeyModifiers.Control | KeyModifiers.Shift, "", "Tools")]
	public RxCommandUnit? StopSpeakingCommand { get; private set; }

	[Keybinding("Check for Updates...", Key.F7, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? CheckForUpdatesCommand { get; private set; }

	[Keybinding("Donate a Coffee...", Key.F10, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenDonationLinkCommand { get; private set; }

	[Keybinding("Open Repository Page...", Key.F11, KeyModifiers.None, "", "Help")]
	public RxCommandUnit? OpenRepositoryPageCommand { get; private set; }

	private readonly ObservableCollectionExtended<IMenuEntry> _menuEntries = [];

	private readonly ReadOnlyObservableCollection<IMenuEntry> _uiMenuEntries;
	public ReadOnlyObservableCollection<IMenuEntry> MenuEntries => _uiMenuEntries;

	[Reactive] public IModOrderViewModel? ModOrder { get; protected set; }

	[Reactive] public bool HighlightExtenderDownload { get; private set; }

	public MainCommandBarViewModel()
	{
		_menuEntries.ToObservableChangeSet().Bind(out _uiMenuEntries).Subscribe();
	}

	private static bool ToggleWindow<T>() where T : Avalonia.Controls.Window
	{
		var window = AppServices.Get<T>();
		if(window != null)
		{
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
		}
		return false;
	}

	[Obsolete]
	private static void NotImplemented()
	{
		throw new NotImplementedException();
	}

	public void SetExtenderHighlight(bool enabled)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			HighlightExtenderDownload = enabled;
		});
	}

	public MainCommandBarViewModel(MainWindowViewModel main, ModOrderViewModel modOrder, ModImportService modImporter, IFileSystemService fs) : this()
	{
		ModOrder = modOrder;
		var canExecuteCommands = main.WhenAnyValue(x => x.IsLocked, b => !b);

		var isModOrderView = main.WhenAnyValue(x => x.Views.CurrentView, x => x == modOrder);
		var canExecuteModOrderCommands = canExecuteCommands.CombineLatest(isModOrderView).AllTrue();

		var hasActiveMods = modOrder.WhenAnyValue(x => x.TotalActiveMods, x => x > 0);
		var canExecuteHasActive = canExecuteModOrderCommands.CombineLatest(hasActiveMods).AllTrue();

		AddNewOrderCommand = ReactiveCommand.Create(() => modOrder.AddNewModOrder(), canExecuteModOrderCommands);

		CheckForAppUpdatesCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Commands.ShowAlert("Checking for updates...", AlertType.Info, 30);
			//main.CheckForUpdates(true);
			main.SaveSettings();
		}, canExecuteCommands);

		var anyDownloadAllowed = modOrder.WhenAnyValue(x => x.GitHubModSupportEnabled, x => x.NexusModsSupportEnabled, x => x.ModioSupportEnabled).Select(x => x.Item1 || x.Item2 || x.Item3).ObserveOn(RxApp.MainThreadScheduler);

		CheckAllModUpdatesCommand = ReactiveCommand.Create(main.RefreshAllModUpdatesBackground, anyDownloadAllowed.AllTrue(canExecuteCommands));
		CheckForGitHubModUpdatesCommand = ReactiveCommand.Create(main.RefreshGitHubModsUpdatesBackground, modOrder.WhenAnyValue(x => x.GitHubModSupportEnabled).AllTrue(canExecuteCommands));
		CheckForNexusModsUpdatesCommand = ReactiveCommand.Create(main.RefreshNexusModsUpdatesBackground, modOrder.WhenAnyValue(x => x.NexusModsSupportEnabled).AllTrue(canExecuteCommands));
		CheckForModioUpdatesCommand = ReactiveCommand.Create(main.RefreshModioUpdatesBackground, modOrder.WhenAnyValue(x => x.ModioSupportEnabled).AllTrue(canExecuteCommands));

		ExportOrderCommand = ReactiveCommand.CreateFromTask(modOrder.ExportLoadOrderAsync, canExecuteCommands);

		LaunchGameCommand = ReactiveCommand.Create(main.LaunchGame, main.WhenAnyValue(x => x.CanLaunchGame).AllTrue(canExecuteCommands));

		OpenGitHubRepoCommand = ReactiveCommand.Create(() => AppServices.Commands.OpenURL(DivinityApp.URL_REPO), canExecuteCommands);
		OpenDonationPageCommand = ReactiveCommand.Create(() => AppServices.Commands.OpenURL(DivinityApp.URL_DONATION), canExecuteCommands);
		OpenModsFolderCommand = ReactiveCommand.Create(() => ProcessHelper.TryOpenPath(main.PathwayData.AppDataModsPath, fs.Directory.Exists), canExecuteCommands);

		var gameExecutableExists = main.Settings.WhenAnyValue(x => x.GameExecutablePath, fs.File.Exists);
		var logFolderExists = main.Settings.WhenAnyValue(x => x.ExtenderLogDirectory).Select(fs.Directory.Exists);
		var canOpenLogsFolder = canExecuteCommands.CombineLatest(gameExecutableExists, logFolderExists).AllTrue();

		OpenLogsFolderCommand = ReactiveCommand.Create(() =>
		{
			if(fs.File.Exists(main.Settings.GameExecutablePath))
			{
				return ProcessHelper.TryOpenPath(fs.Path.GetDirectoryName(main.Settings.GameExecutablePath), fs.Directory.Exists);
			}
			return false;
		}, canOpenLogsFolder);

		OpenGameFolderCommand = ReactiveCommand.Create(() =>
		{
			if(fs.File.Exists(main.Settings.GameExecutablePath))
			{
				return ProcessHelper.TryOpenPath(fs.Path.GetDirectoryName(main.Settings.GameExecutablePath));
			}
			return false;
		}, canExecuteCommands.CombineLatest(gameExecutableExists).AllTrue());

		//Savegames\Story
		var canOpenProfileSaves = modOrder.WhenAnyValue(x => x.SelectedProfileSavesPath, fs.Directory.Exists).CombineLatest(canExecuteCommands).AllTrue();
		OpenSavesFolderCommand = ReactiveCommand.Create(() => ProcessHelper.TryOpenPath(modOrder.SelectedProfileSavesPath), canOpenProfileSaves);

		var canOpenExtenderDirectory = main.PathwayData.WhenAnyValue(x => x.AppDataGameFolder).Select(x => fs.Directory.Exists(fs.Path.Join(x, "Script Extender")));
		OpenExtenderDataFolderCommand = ReactiveCommand.Create(() =>
		{
			var extenderDirectory = fs.Path.Join(main.PathwayData.AppDataGameFolder, "Script Extender");
			ProcessHelper.TryOpenPath(extenderDirectory);
		}, canOpenExtenderDirectory);

		OpenNexusModsCommand = ReactiveCommand.Create(() => ProcessHelper.TryOpenPath(DivinityApp.URL_NEXUSMODS), canExecuteCommands);
		OpenSteamPageCommand = ReactiveCommand.Create(() => ProcessHelper.TryOpenPath(DivinityApp.URL_STEAM), canExecuteCommands);

		var canRefreshModUpdates = canExecuteCommands.CombineLatest(main.WhenAnyValue(x => x.IsRefreshingModUpdates, x => x.AppSettingsLoaded))
			.Select(x => x.First && !x.Second.Item1 && x.Second.Item2);

		RefreshCommand = ReactiveCommand.Create(main.RefreshStart, canExecuteCommands);
		RefreshModUpdatesCommand = ReactiveCommand.Create(main.RefreshModUpdates, canRefreshModUpdates);

		RenameSaveCommand = ReactiveCommand.CreateFromTask(main.RenameSaveAsync, canExecuteCommands);

		SaveOrderCommand = ReactiveCommand.CreateFromTask(modOrder.SaveLoadOrderAsync, canExecuteCommands);
		SaveOrderAsCommand = ReactiveCommand.CreateFromTask(modOrder.SaveLoadOrderAsAsync, canExecuteCommands);
		SaveSettingsSilentlyCommand = ReactiveCommand.Create(main.SaveSettings, canExecuteCommands);

		var canToggleUpdatesView = canExecuteCommands.CombineLatest(main.WhenAnyValue(x => x.ModUpdatesAvailable)).AllTrue();

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

		TogglePakFileExplorerWindowCommand = ReactiveCommand.Create(ToggleWindow<PakFileExplorerWindow>, canExecuteCommands);
		ToggleVersionGeneratorWindowCommand = ReactiveCommand.Create(ToggleWindow<VersionGeneratorWindow>, canExecuteCommands);
		ToggleStatsValidatorWindowCommand = ReactiveCommand.Create(ToggleWindow<StatsValidatorWindow>, canExecuteCommands);
		ToggleSettingsWindowCommand = ReactiveCommand.Create(ToggleWindow<SettingsWindow>, canExecuteCommands);
		ToggleAboutWindowCommand = ReactiveCommand.Create(ToggleWindow<AboutWindow>, canExecuteCommands);

		ToggleKeybindingsCommand = ReactiveCommand.Create(() =>
		{
			var result = false;
			ToggleSettingsWindowCommand.Execute().Subscribe(isVisible =>
			{
				if (isVisible)
				{
					var vm = AppServices.Get<SettingsWindowViewModel>();
					vm.SelectedTabIndex = SettingsWindowTab.Keybindings;
					result = true;
				}
			});
			return result;
		}, canExecuteCommands);

		ToggleNXMLinkDownloaderCommand = ReactiveCommand.Create(ToggleWindow<NxmDownloadWindow>, canExecuteCommands);
		ToggleCollectionDownloaderWindowCommand = ReactiveCommand.Create(ToggleWindow<NexusModsCollectionDownloadWindow>, canExecuteCommands);

		ToggleThemeModeCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Settings.ManagerSettings.DarkThemeEnabled = !AppServices.Settings.ManagerSettings.DarkThemeEnabled;
		}, canExecuteCommands);

		ImportModCommand = ReactiveCommand.CreateFromTask(modImporter.OpenModImportDialog, canExecuteCommands);
		ImportNexusModsIdsCommand = ReactiveCommand.CreateFromTask(modImporter.OpenModIdsImportDialog, canExecuteCommands);

		ImportOrderFromSaveCommand = ReactiveCommand.CreateFromTask(modOrder.ImportOrderFromSaveToCurrent, canExecuteCommands);
		ImportOrderFromSaveAsNewCommand = ReactiveCommand.CreateFromTask(modOrder.ImportOrderFromSaveAsNew, canExecuteCommands);
		ImportOrderFromFileCommand = ReactiveCommand.CreateFromTask(modOrder.ImportOrderFromFile, canExecuteCommands);
		ImportOrderFromZipFileCommand = ReactiveCommand.CreateFromTask(modImporter.ImportOrderFromArchive, canExecuteCommands);

		ExportOrderToTextFileCommand = ReactiveCommand.CreateFromTask(modOrder.ExportLoadOrderToTextFileAsAsync, canExecuteHasActive);
		ExportOrderToZipCommand = ReactiveCommand.CreateFromTask(main.ExportLoadOrderToArchiveAsync, canExecuteHasActive);
		ExportOrderToArchiveAsCommand = ReactiveCommand.CreateFromTask(main.ExportLoadOrderToArchiveAsAsync, canExecuteHasActive);

		var canRenameOrder = modOrder.WhenAnyValue(x => x.IsModSettingsOrder, x => x.SelectedModOrder, (b, order) => !b && order != null);
		RenameOrderCommand = ReactiveCommand.CreateFromTask(async () =>
		{
			var order = modOrder.SelectedModOrder!;
			var orderPath = order.FilePath!;
			var fileName = fs.Path.GetFileNameWithoutExtension(orderPath);
			var directory = fs.Path.GetDirectoryName(orderPath);
			var ext = fs.Path.GetExtension(orderPath);

			var request = new ShowMessageBoxRequest($"Rename {fileName}", $"Input a new name for {orderPath}. This will update the file path as well.", InteractionMessageBoxType.Input, fileName);
			var result = await AppServices.Interactions.ShowMessageBox.Handle(request);

			if (result.Result && result.Input.IsValid())
			{
				var nextName = fs.Path.GetFileNameWithoutExtension(result.Input);
				var nextFilePath = fs.Path.Join(directory, ModDataLoader.MakeSafeFilename(nextName + ext, '_'));

				fs.File.Move(orderPath, nextFilePath, true);
				var existingOrder = modOrder.ExternalModOrders.FirstOrDefault(x => x.FilePath == nextFilePath);
				if (existingOrder != null)
				{
					modOrder.ExternalModOrders.Remove(existingOrder);
				}
				order.Name = nextName;
				order.FilePath = nextFilePath;
				AppServices.Commands.ShowAlert($"Renamed load order to '{nextFilePath}'", AlertType.Success, 20);
			}
		}, canExecuteCommands.CombineLatest(canRenameOrder).AllTrue());

		//TODO
		ReloadModsCommand = ReactiveCommand.Create(() => { }, canExecuteModOrderCommands);

		MoveSelectedModsCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;

			ObservableCollectionExtended<IModEntry>? sourceCollection = null;
			ModListViewModel? sourceList = null;
			ModListViewModel? targetList = null;
			string? targetListName = null;
			
			if(modOrder.ActiveModsView.HasAnyFocus)
			{
				sourceCollection = modOrder.ActiveMods;
				sourceList = modOrder.ActiveModsView;
				targetList = modOrder.InactiveModsView;
				targetListName = "inactive";
			}
			else if(modOrder.InactiveModsView.HasAnyFocus)
			{
				sourceCollection = modOrder.InactiveMods;
				sourceList = modOrder.InactiveModsView;
				targetList = modOrder.ActiveModsView;
				targetListName = "active";
			}

			if (sourceCollection != null && sourceList != null && targetList != null && targetListName != null)
			{
				var selectedMods = sourceCollection.Where(x => x.IsSelected).ToList();

				if (selectedMods.Count <= 0) return;

				var selectedMod = selectedMods[0];
				var nextSelectedIndex = sourceCollection.IndexOf(selectedMod);

				foreach (var mod in selectedMods)
				{
					if (mod != null) mod.PreserveSelection = true;
				}

				var targetIndex = targetList.Mods.RowSelection.SelectedIndex;
				//Clear the previous selection, so only the dropped items are selected
				targetList.Mods.RowSelection!.Clear();

				ModListView.DragDropRows(sourceList.Mods, targetList.Mods,
					sourceList.Mods.RowSelection!.SelectedIndexes,
					targetIndex,
					TreeDataGridRowDropPosition.After, DragDropEffects.Move);

				string countSuffix = selectedMods.Count > 1 ? "mods" : "mod";
				string text = $"Moved {selectedMods.Count} {countSuffix} to the {targetListName} mods list.";
				if (DivinityApp.IsScreenReaderActive()) AppServices.ScreenReader.Speak(text);
				AppServices.Commands.ShowAlert(text, AlertType.Info, 10);
				modOrder.CanMoveSelectedMods = false;

				if (main.Settings.ShiftListFocusOnSwap)
				{
					targetList.FocusCommand.Execute().Subscribe();
				}
			}
		}, canExecuteModOrderCommands);

		FocusActiveModsCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;

			var modOrderView = AppServices.Get<ModOrderView>()!;

			modOrder.ActiveModsView.FocusCommand.Execute().Subscribe();

			if (modOrder.ActiveModsView.TotalModsSelected == 0)
			{
				modOrderView.ActiveModsList.ModsTreeDataGrid.RowSelection?.Select(0);
			}
			//TODO FocusSelectedItem?
		}, canExecuteModOrderCommands);

		FocusInactiveModsCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;

			var modOrderView = AppServices.Get<ModOrderView>()!;

			modOrder.InactiveModsView.FocusCommand.Execute().Subscribe();

			if (modOrder.InactiveModsView.TotalModsSelected == 0)
			{
				modOrderView.InactiveModsList.ModsTreeDataGrid.RowSelection?.Select(0);
			}
			//TODO FocusSelectedItem?
		}, canExecuteModOrderCommands);

		SwapListFocusCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;
			if (modOrder.ActiveModsView.HasAnyFocus)
			{
				modOrder.InactiveModsView.FocusCommand.Execute().Subscribe();
			}
			else if (modOrder.InactiveModsView.HasAnyFocus)
			{
				modOrder.ActiveModsView.FocusCommand.Execute().Subscribe();
			}
		}, canExecuteModOrderCommands);

		MoveToTopCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;
			if (modOrder.ActiveModsView.HasAnyFocus)
			{
				modOrder.ActiveModsView.Mods.RowSelection?.Select(0);
			}
			else if (modOrder.InactiveModsView.HasAnyFocus)
			{
				modOrder.InactiveModsView.Mods.RowSelection?.Select(0);
			}
		}, canExecuteModOrderCommands);

		MoveToBottomCommand = ReactiveCommand.Create(() =>
		{
			DivinityApp.IsKeyboardNavigating = true;
			if (modOrder.ActiveModsView.HasAnyFocus)
			{
				modOrder.ActiveModsView.Mods.RowSelection?.Select(modOrder.ActiveModsView.Mods.Rows.Count-1);
			}
			else if (modOrder.InactiveModsView.HasAnyFocus)
			{
				modOrder.InactiveModsView.Mods.RowSelection?.Select(modOrder.InactiveModsView.Mods.Rows.Count - 1);
			}
		}, canExecuteModOrderCommands);

		ToggleFilterFocusCommand = ReactiveCommand.Create(() =>
		{
			var modOrderView = AppServices.Get<ModOrderView>()!;

			if (modOrder.ActiveModsView.HasAnyFocus)
			{
				if(!modOrderView.ActiveModsList.FilterTextBox.IsFocused)
				{
					modOrderView.ActiveModsList.FilterTextBox.Focus();
				}
				else
				{
					modOrder.ActiveModsView.FocusCommand.Execute().Subscribe();
				}
			}
			else if (modOrder.InactiveModsView.HasAnyFocus)
			{
				if (!modOrderView.InactiveModsList.FilterTextBox.IsFocused)
				{
					modOrderView.InactiveModsList.FilterTextBox.Focus();
				}
				else
				{
					modOrder.InactiveModsView.FocusCommand.Execute().Subscribe();
				}
			}
		}, canExecuteModOrderCommands);

		ToggleFileNameDisplayCommand = ReactiveCommand.Create(() =>
		{
			main.Settings.DisplayFileNames = !main.Settings.DisplayFileNames;

			foreach (var m in AppServices.Mods.AllMods)
			{
				m.DisplayFileForName = main.Settings.DisplayFileNames;
			}
		}, canExecuteModOrderCommands);

		var totalActiveSelected = modOrder.ActiveModsView.WhenAnyValue(x => x.TotalModsSelected, count => count > 0);
		var totalInactiveSelected = modOrder.InactiveModsView.WhenAnyValue(x => x.TotalModsSelected, count => count > 0);
		var anySelected = totalActiveSelected.CombineLatest(totalInactiveSelected).Select(x => x.First || x.Second);
		var canDeleteMods = canExecuteModOrderCommands.CombineLatest(anySelected).Select(x => x.First && x.Second);

		DeleteSelectedModsCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Commands.DeleteSelectedModsCommand.Execute().Subscribe();
		}, canDeleteMods);

		var keybindings = this.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetCustomAttribute<KeybindingAttribute>());

		DownloadScriptExtenderCommand = ReactiveCommand.Create(main.AskToDownloadScriptExtender, canExecuteCommands);
		ExtractAllSelectedModsCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);
		ExtractSelectedActiveModsCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);
		ExtractSelectedInactiveModsCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);
		ExtractSelectedAdventureCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);
		SpeakActiveModOrderCommand = ReactiveCommand.Create(() =>
		{
			var speaker = AppServices.Get<IScreenReaderService>();
			if(speaker != null)
			{
				if (ModOrder.ActiveMods.Count > 0)
				{
					var text = string.Join(", ", ModOrder.ActiveMods.Select(x => x.DisplayName));
					speaker.Speak($"{ModOrder.ActiveMods.Count} mods are in the active order, including:\n{text}", true);
				}
				else
				{
					speaker.Speak($"Zero mods are active.", true);
				}
			}
		}, canExecuteCommands);
		StopSpeakingCommand = ReactiveCommand.Create(() =>
		{
			AppServices.Get<IScreenReaderService>()?.Silence();
		}, canExecuteCommands);
		CheckForUpdatesCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);
		OpenDonationLinkCommand = ReactiveCommand.Create(NotImplemented, canExecuteCommands);

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
					MenuEntry.FromKeybinding(RenameOrderCommand, nameof(RenameOrderCommand), keybindings),
					MenuEntry.FromKeybinding(RenameSaveCommand, nameof(RenameSaveCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(AddNewOrderCommand, nameof(AddNewOrderCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ImportOrderFromSaveCommand, nameof(ImportOrderFromSaveCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromSaveAsNewCommand, nameof(ImportOrderFromSaveAsNewCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromFileCommand, nameof(ImportOrderFromFileCommand), keybindings),
					MenuEntry.FromKeybinding(ImportOrderFromZipFileCommand, nameof(ImportOrderFromZipFileCommand), keybindings),
					new MenuSeparator(),
					MenuEntry.FromKeybinding(ExportOrderCommand, nameof(ExportOrderCommand), keybindings),
					MenuEntry.FromKeybinding(ExportOrderToTextFileCommand, nameof(ExportOrderToTextFileCommand), keybindings),
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
					MenuEntry.FromKeybinding(ToggleNXMLinkDownloaderCommand, nameof(ToggleNXMLinkDownloaderCommand), keybindings),
					MenuEntry.FromKeybinding(ToggleCollectionDownloaderWindowCommand, nameof(ToggleCollectionDownloaderWindowCommand), keybindings),
					new MenuSeparator(),
					new MenuEntry("Check for Mod Updates..."){
					Children = [
						MenuEntry.FromKeybinding(CheckAllModUpdatesCommand, nameof(CheckAllModUpdatesCommand), keybindings),
						new MenuSeparator(),
						MenuEntry.FromKeybinding(CheckForGitHubModUpdatesCommand, nameof(CheckForGitHubModUpdatesCommand), keybindings),
						MenuEntry.FromKeybinding(CheckForNexusModsUpdatesCommand, nameof(CheckForNexusModsUpdatesCommand), keybindings),
						MenuEntry.FromKeybinding(CheckForModioUpdatesCommand, nameof(CheckForModioUpdatesCommand), keybindings)
					]},
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
					MenuEntry.FromKeybinding(ToggleAboutWindowCommand, nameof(ToggleAboutWindowCommand), keybindings),
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