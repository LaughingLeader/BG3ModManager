using ModManager.Models.Mod;

using ReactiveUI;

using System.Reactive;

namespace ModManager;

public interface IGlobalCommandsService
{
	ReactiveCommand<string?, Unit> OpenFileCommand { get; }
	ReactiveCommand<string?, Unit> OpenInFileExplorerCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> ToggleNameDisplayCommand { get; }
	ReactiveCommand<string?, Unit> CopyToClipboardCommand { get; }
	ReactiveCommand<IModEntry?, Unit> DeleteModCommand { get; }
	RxCommandUnit DeleteSelectedModsCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> OpenGitHubPageCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> OpenNexusModsPageCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> OpenSteamWorkshopPageCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> OpenSteamWorkshopPageInSteamCommand { get; }
	ReactiveCommand<object?, Unit> OpenURLCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> ToggleForceAllowInLoadOrderCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> CopyModAsDependencyCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> OpenModPropertiesCommand { get; }
	ReactiveCommand<DivinityModData?, Unit> ValidateStatsCommand { get; }

	void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
	Task ShowAlertAsync(string message, AlertType alertType = AlertType.Info, int timeout = 0);
}
