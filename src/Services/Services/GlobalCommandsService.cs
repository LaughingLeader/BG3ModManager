using ModManager.Models.Mod;
using ModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;

using TextCopy;

namespace ModManager.Services;

public class GlobalCommandsService : ReactiveObject, IGlobalCommandsService
{
	private readonly IInteractionsService _interactions;
	private readonly IFileSystemService _fs;

	[Reactive] public bool CanExecuteCommands { get; set; }

	public ReactiveCommand<string?, Unit> OpenFileCommand { get; }
	public ReactiveCommand<string?, Unit> OpenInFileExplorerCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> ToggleNameDisplayCommand { get; }
	public ReactiveCommand<string?, Unit> CopyToClipboardCommand { get; }
	public ReactiveCommand<IModEntry?, Unit> DeleteModCommand { get; }
	public RxCommandUnit DeleteSelectedModsCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> OpenGitHubPageCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> OpenNexusModsPageCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> OpenSteamWorkshopPageCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> OpenSteamWorkshopPageInSteamCommand { get; }
	public ReactiveCommand<object?, Unit> OpenURLCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> ToggleForceAllowInLoadOrderCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> CopyModAsDependencyCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> OpenModPropertiesCommand { get; }
	public ReactiveCommand<DivinityModData?, Unit> ValidateStatsCommand { get; }

	private void OpenFile(string? path)
	{
		if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), "path is null or empty");

		if (_fs.File.Exists(path))
		{
			try
			{
				Process.Start(_fs.Path.GetFullPath(path));
			}
			catch (System.ComponentModel.Win32Exception) // No File Association
			{
				Process.Start("explorer.exe", $"\"{_fs.Path.GetFullPath(path)}\"");
			}
		}
		else if (_fs.Directory.Exists(path))
		{
			Process.Start("explorer.exe", $"\"{_fs.Path.GetFullPath(path)}\"");
		}
		else
		{
			ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
		}
	}

	private void OpenInFileExplorer(string? path)
	{
		if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), "path is null or empty");

		if (_fs.File.Exists(path))
		{
			Process.Start("explorer.exe", $"/select, \"{_fs.Path.GetFullPath(path)}\"");
		}
		else if (_fs.Directory.Exists(path))
		{
			Process.Start("explorer.exe", $"\"{_fs.Path.GetFullPath(path)}\"");
		}
		else
		{
			ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
		}
	}

	private void CopyToClipboard(string? text)
	{
		if (text == null) throw new ArgumentNullException(nameof(text), "text to copy is null");
		try
		{
			ClipboardService.SetText(text);
			ShowAlert($"Copied to clipboard: {text}", 0, 10);
		}
		catch (Exception ex)
		{
			ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
		}
	}

	private void CopyModAsDependency(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		try
		{
			var safeName = System.Security.SecurityElement.Escape(mod.Name);
			var text = string.Format(DivinityApp.XML_MODULE_SHORT_DESC_FORMATTED, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt);
			ClipboardService.SetText(text);
			ShowAlert($"Copied ModuleShortDesc for mod '{mod.Name}' to clipboard", 0, 10);
		}
		catch (Exception ex)
		{
			ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
		}
	}

	private static void OpenURL(string url)
	{
		if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
		FileUtils.TryOpenPath(url);
	}

	private static void OpenGitHubPage(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.GITHUB);
		if (!string.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	private static void OpenNexusModsPage(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.NEXUSMODS);
		if (!string.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	private static void OpenSteamWorkshopPage(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.STEAM);
		if (!string.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	private static void OpenSteamWorkshopPageInSteam(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.STEAM, true);
		if (!string.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	private static void ToggleForceAllowInLoadOrder(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		mod.ForceAllowInLoadOrder = !mod.ForceAllowInLoadOrder;
	}

	private void OpenModProperties(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		_interactions.OpenModProperties.Handle(mod).Subscribe();
	}

	private void StartValidateModStats(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		_interactions.ValidateModStats.Handle(new([mod], CancellationToken.None)).Subscribe();
	}

	private void ToggleNameDisplay(DivinityModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var b = !mod.DisplayFileForName;
		_interactions.ToggleModFileNameDisplay.Handle(b).Subscribe();
	}

	private void DeleteMod(IModEntry? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		_interactions.DeleteMods.Handle(new([mod])).Subscribe();
	}

	private void DeleteSelectedMods()
	{
		_interactions.DeleteSelectedMods.Handle(Unit.Default).Subscribe();
	}

	public void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		_interactions.ShowAlert.Handle(new(message, alertType, timeout)).Subscribe();
	}

	public async Task ShowAlertAsync(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		await _interactions.ShowAlert.Handle(new(message, alertType, timeout));
	}

	public GlobalCommandsService(IInteractionsService interactionsService, IFileSystemService fileSystemService)
	{
		_interactions = interactionsService;
		_fs = fileSystemService;

		var canExecuteCommands = this.WhenAnyValue(x => x.CanExecuteCommands).ObserveOn(RxApp.MainThreadScheduler);

		OpenFileCommand = ReactiveCommand.Create<string?>(OpenFile, canExecuteCommands);
		OpenInFileExplorerCommand = ReactiveCommand.Create<string?>(OpenInFileExplorer, canExecuteCommands);

		ToggleNameDisplayCommand = ReactiveCommand.Create<DivinityModData?>(ToggleNameDisplay, canExecuteCommands);

		CopyToClipboardCommand = ReactiveCommand.Create<string?>(CopyToClipboard, canExecuteCommands);

		DeleteModCommand = ReactiveCommand.Create<IModEntry?>(DeleteMod, canExecuteCommands);

		DeleteSelectedModsCommand = ReactiveCommand.Create(DeleteSelectedMods, canExecuteCommands);

		OpenURLCommand = ReactiveCommand.Create<object?>(x => OpenURL(x?.ToString()), canExecuteCommands);
		OpenGitHubPageCommand = ReactiveCommand.Create<DivinityModData?>(OpenGitHubPage, canExecuteCommands);
		OpenNexusModsPageCommand = ReactiveCommand.Create<DivinityModData?>(OpenNexusModsPage, canExecuteCommands);
		OpenSteamWorkshopPageCommand = ReactiveCommand.Create<DivinityModData?>(OpenSteamWorkshopPage, canExecuteCommands);
		OpenSteamWorkshopPageInSteamCommand = ReactiveCommand.Create<DivinityModData?>(OpenSteamWorkshopPageInSteam, canExecuteCommands);
		ToggleForceAllowInLoadOrderCommand = ReactiveCommand.Create<DivinityModData?>(ToggleForceAllowInLoadOrder, canExecuteCommands);
		CopyModAsDependencyCommand = ReactiveCommand.Create<DivinityModData?>(CopyModAsDependency, canExecuteCommands);
		OpenModPropertiesCommand = ReactiveCommand.Create<DivinityModData?>(OpenModProperties, canExecuteCommands);
		ValidateStatsCommand = ReactiveCommand.Create<DivinityModData?>(StartValidateModStats, canExecuteCommands);
	}
}
