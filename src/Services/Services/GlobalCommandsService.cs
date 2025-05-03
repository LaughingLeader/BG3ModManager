using ModManager.Models.Mod;
using ModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

using TextCopy;

namespace ModManager.Services;

public class GlobalCommandsService : ReactiveObject, IGlobalCommandsService
{
	private readonly IInteractionsService _interactions;
	private readonly IFileSystemService _fs;

	[Reactive] public bool CanExecuteCommands { get; set; }

	public ReactiveCommand<string?, Unit> OpenFileCommand { get; }
	public ReactiveCommand<string?, Unit> OpenInFileExplorerCommand { get; }
	public ReactiveCommand<ModData?, Unit> ToggleNameDisplayCommand { get; }
	public ReactiveCommand<string?, Unit> CopyToClipboardCommand { get; }
	public ReactiveCommand<IModEntry?, Unit> DeleteModCommand { get; }
	public RxCommandUnit DeleteSelectedModsCommand { get; }
	public ReactiveCommand<ModData?, Unit> OpenGitHubPageCommand { get; }
	public ReactiveCommand<ModData?, Unit> OpenNexusModsPageCommand { get; }
	public ReactiveCommand<ModData?, Unit> OpenModioPageCommand { get; }
	public ReactiveCommand<object?, Unit> OpenURLCommand { get; }
	public ReactiveCommand<ModData?, Unit> ToggleForceAllowInLoadOrderCommand { get; }
	public ReactiveCommand<ModData?, Unit> CopyModAsDependencyCommand { get; }
	public ReactiveCommand<ModData?, Unit> OpenModPropertiesCommand { get; }
	public ReactiveCommand<ModData?, Unit> ValidateStatsCommand { get; }

	private void OpenFile(string? path)
	{
		if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path), "path is null or empty");

		path = _fs.GetRealPath(path);

		if (_fs.File.Exists(path))
		{
			try
			{
				Process.Start(path);
			}
			catch (System.ComponentModel.Win32Exception) // No File Association
			{
				Process.Start("explorer.exe", $"\"{path}\"");
			}
		}
		else if (_fs.Directory.Exists(path))
		{
			Process.Start("explorer.exe", $"\"{path}\"");
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

	private void CopyModAsDependency(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		try
		{
			var safeName = System.Security.SecurityElement.Escape(mod.Name);
			var text = string.Format(DivinityApp.XML_MODULE_SHORT_DESC_FORMATTED, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt, mod.PublishHandle);
			ClipboardService.SetText(text);
			ShowAlert($"Copied ModuleShortDesc for mod '{mod.Name}' to clipboard", 0, 10);
		}
		catch (Exception ex)
		{
			ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
		}
	}

	public void OpenURL(string? url)
	{
		if (!url.IsValid()) throw new ArgumentNullException(nameof(url));
		//Source: https://stackoverflow.com/a/43232486
		try
		{
			Process.Start(url);
		}
		catch
		{
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				url = url.Replace("&", "^&");
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", url);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", url);
			}
			else
			{
				throw;
			}
		}
	}

	private void OpenGitHubPage(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.GITHUB);
		OpenURL(url);
	}

	private void OpenNexusModsPage(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.NEXUSMODS);
		OpenURL(url);
	}

	private void OpenModioPage(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		var url = mod.GetURL(ModSourceType.MODIO);
		OpenURL(url);
	}

	private static void ToggleForceAllowInLoadOrder(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		mod.ForceAllowInLoadOrder = !mod.ForceAllowInLoadOrder;
	}

	private void OpenModProperties(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		_interactions.OpenModProperties.Handle(mod).Subscribe();
	}

	private void StartValidateModStats(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		_interactions.ValidateModStats.Handle(new([mod], CancellationToken.None)).Subscribe();
	}

	private void ToggleNameDisplay(ModData? mod)
	{
		if (mod == null) throw new ArgumentNullException(nameof(mod));
		mod.DisplayFileForName = !mod.DisplayFileForName;
		//var b = !mod.DisplayFileForName;
		//_interactions.ToggleModFileNameDisplay.Handle(b).Subscribe();
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

	public void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0, string? title = "")
	{
		_interactions.ShowAlert.Handle(new(StringUtils.ReplaceSpecialPathways(message), alertType, timeout, title)).Subscribe();
	}

	public async Task ShowAlertAsync(string message, AlertType alertType = AlertType.Info, int timeout = 0, string? title = "")
	{
		await _interactions.ShowAlert.Handle(new(StringUtils.ReplaceSpecialPathways(message), alertType, timeout, title));
	}

	public GlobalCommandsService(IInteractionsService interactionsService, IFileSystemService fileSystemService)
	{
		_interactions = interactionsService;
		_fs = fileSystemService;

		var canExecuteCommands = this.WhenAnyValue(x => x.CanExecuteCommands).ObserveOn(RxApp.MainThreadScheduler);

		OpenFileCommand = ReactiveCommand.Create<string?>(OpenFile, canExecuteCommands);
		OpenInFileExplorerCommand = ReactiveCommand.Create<string?>(OpenInFileExplorer, canExecuteCommands);

		ToggleNameDisplayCommand = ReactiveCommand.Create<ModData?>(ToggleNameDisplay, canExecuteCommands);

		CopyToClipboardCommand = ReactiveCommand.Create<string?>(CopyToClipboard, canExecuteCommands);

		DeleteModCommand = ReactiveCommand.Create<IModEntry?>(DeleteMod, canExecuteCommands);

		DeleteSelectedModsCommand = ReactiveCommand.Create(DeleteSelectedMods, canExecuteCommands);

		OpenURLCommand = ReactiveCommand.Create<object?>(x => OpenURL(x?.ToString()), canExecuteCommands);
		OpenGitHubPageCommand = ReactiveCommand.Create<ModData?>(OpenGitHubPage, canExecuteCommands);
		OpenNexusModsPageCommand = ReactiveCommand.Create<ModData?>(OpenNexusModsPage, canExecuteCommands);
		OpenModioPageCommand = ReactiveCommand.Create<ModData?>(OpenModioPage, canExecuteCommands);
		ToggleForceAllowInLoadOrderCommand = ReactiveCommand.Create<ModData?>(ToggleForceAllowInLoadOrder, canExecuteCommands);
		CopyModAsDependencyCommand = ReactiveCommand.Create<ModData?>(CopyModAsDependency, canExecuteCommands);
		OpenModPropertiesCommand = ReactiveCommand.Create<ModData?>(OpenModProperties, canExecuteCommands);
		ValidateStatsCommand = ReactiveCommand.Create<ModData?>(StartValidateModStats, canExecuteCommands);
	}
}
