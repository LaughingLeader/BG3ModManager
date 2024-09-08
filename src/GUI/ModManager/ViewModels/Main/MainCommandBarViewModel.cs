using ModManager.Util;

namespace ModManager.ViewModels.Main;

[Keybindings]
public partial class MainCommandBarViewModel : ReactiveObject
{
	[Keybinding("Add New Order", Key.M)]
	public RxCommandUnit? AddNewOrderCommand { get; set; }

	[Keybinding("Check For App Updates", Key.U)]
	public RxCommandUnit? CheckForAppUpdatesCommand { get; set; }

	public RxCommandUnit? CheckForGitHubModUpdatesCommand { get; set; }

	public RxCommandUnit? CheckForNexusModsUpdatesCommand { get; set; }

	public RxCommandUnit? CheckForSteamWorkshopUpdatesCommand { get; set; }

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

		var x = main.WhenAnyValue(x => x.GitHubModSupportEnabled).CombineLatest(canExecuteCommands).Select(x => x.First);

		CheckForGitHubModUpdatesCommand = ReactiveCommand.Create(main.RefreshGitHubModsUpdatesBackground, main.WhenAnyValue(x => x.GitHubModSupportEnabled).AllTrue(canExecuteCommands));
		CheckForNexusModsUpdatesCommand = ReactiveCommand.Create(main.RefreshNexusModsUpdatesBackground, main.WhenAnyValue(x => x.NexusModsSupportEnabled).AllTrue(canExecuteCommands));
		CheckForSteamWorkshopUpdatesCommand = ReactiveCommand.Create(main.RefreshModioUpdatesBackground, main.WhenAnyValue(x => x.ModioSupportEnabled).AllTrue(canExecuteCommands));

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

		this.RegisterKeybindings();
	}

	public MainCommandBarViewModel()
	{
		
	}
}