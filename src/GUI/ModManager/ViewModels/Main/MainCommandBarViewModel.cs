namespace ModManager.ViewModels.Main;
public class MainCommandBarViewModel : ReactiveObject
{
	public RxCommandUnit RefreshCommand { get; }
	public RxCommandUnit CancelMainProgressCommand { get; }
	public RxCommandUnit ToggleUpdatesViewCommand { get; }
	public RxCommandUnit CheckForAppUpdatesCommand { get; set; }
	public RxCommandUnit RenameSaveCommand { get; }
	public RxCommandUnit SaveSettingsSilentlyCommand { get; }
	public RxCommandUnit RefreshModUpdatesCommand { get; }
	public RxCommandUnit CheckForGitHubModUpdatesCommand { get; }
	public RxCommandUnit CheckForNexusModsUpdatesCommand { get; }
	public RxCommandUnit CheckForSteamWorkshopUpdatesCommand { get; }

	private readonly IGlobalCommandsService _globalCommands;
	public MainCommandBarViewModel(IGlobalCommandsService globalCommands)
	{
		_globalCommands = globalCommands;


	}
}
