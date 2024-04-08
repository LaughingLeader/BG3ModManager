using ModManager.Util;

namespace ModManager.ViewModels.Main;


public partial class MainCommandBarViewModel : ReactiveObject
{
	[Keybinding("Add New Order", Key.M)]
	public RxCommandUnit? AddNewOrderCommand { get; set; }
	//[Keybinding("Check For App Updates", Key.U)]
	public RxCommandUnit? CheckForAppUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForGitHubModUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForNexusModsUpdatesCommand { get; set; }
	public RxCommandUnit? CheckForSteamWorkshopUpdatesCommand { get; set; }
	public RxCommandUnit? ExportModsToZipAsCommand { get; set; }
	public RxCommandUnit? ExportModsToZipCommand { get; set; }
	public ReactiveCommand<Unit,bool>? ExportOrderCommand { get; set; }
	public RxCommandUnit? LaunchGameCommand { get; set; }
	public RxCommandUnit? OpenDonationPageCommand { get; set; }
	public RxCommandUnit? OpenGitHubRepoCommand { get; set; }
	public RxCommandUnit? OpenModsFolderCommand { get; set; }
	public RxCommandUnit? OpenNexusModsCommand { get; set; }
	public RxCommandUnit? OpenSteamPageCommand { get; set; }
	public RxCommandUnit? RefreshCommand { get; set; }
	public RxCommandUnit? RefreshModUpdatesCommand { get; set; }
	public RxCommandUnit? RenameSaveCommand { get; set; }
	public ReactiveCommand<Unit, bool>? SaveOrderCommand { get; set; }
	public ReactiveCommand<Unit, bool>? SaveOrderAsCommand { get; set; }
	public RxCommandUnit? SaveSettingsSilentlyCommand { get; set; }
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
		CheckForSteamWorkshopUpdatesCommand = ReactiveCommand.Create(main.RefreshSteamWorkshopUpdatesBackground, main.WhenAnyValue(x => x.SteamWorkshopSupportEnabled).AllTrue(canExecuteCommands));

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
