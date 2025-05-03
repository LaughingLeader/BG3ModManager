using ModManager.Models.Mod;

namespace ModManager;

public interface IGlobalCommandsService
{
	bool CanExecuteCommands { get; set; }

	ReactiveCommand<string?, Unit> OpenFileCommand { get; }
	ReactiveCommand<string?, Unit> OpenInFileExplorerCommand { get; }
	ReactiveCommand<ModData?, Unit> ToggleNameDisplayCommand { get; }
	ReactiveCommand<string?, Unit> CopyToClipboardCommand { get; }
	ReactiveCommand<IModEntry?, Unit> DeleteModCommand { get; }
	RxCommandUnit DeleteSelectedModsCommand { get; }
	ReactiveCommand<ModData?, Unit> OpenGitHubPageCommand { get; }
	ReactiveCommand<ModData?, Unit> OpenNexusModsPageCommand { get; }
	ReactiveCommand<ModData?, Unit> OpenModioPageCommand { get; }
	ReactiveCommand<object?, Unit> OpenURLCommand { get; }
	ReactiveCommand<ModData?, Unit> ToggleForceAllowInLoadOrderCommand { get; }
	ReactiveCommand<ModData?, Unit> CopyModAsDependencyCommand { get; }
	ReactiveCommand<ModData?, Unit> OpenModPropertiesCommand { get; }
	ReactiveCommand<ModData?, Unit> ValidateStatsCommand { get; }

	void OpenURL(string? url);
	void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0, string? title = "");
	Task ShowAlertAsync(string message, AlertType alertType = AlertType.Info, int timeout = 0, string? title = "");
}
