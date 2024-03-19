using DivinityModManager.Models.Mod;
using DivinityModManager.ViewModels;

using ReactiveUI;

using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;

namespace DivinityModManager.Util;

public class DivinityGlobalCommands : ReactiveObject
{
	private IModOrderViewModel _viewModel;

	public IModOrderViewModel ViewModel => _viewModel;

	public void SetViewModel(IModOrderViewModel vm)
	{
		_viewModel = vm;
		this.RaisePropertyChanged(nameof(ViewModel));
	}

	public ReactiveCommand<string, Unit> OpenFileCommand { get; }
	public ReactiveCommand<string, Unit> OpenInFileExplorerCommand { get; }
	public ReactiveCommand<Unit, Unit> ClearMissingModsCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> ToggleNameDisplayCommand { get; }
	public ReactiveCommand<string, Unit> CopyToClipboardCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> DeleteModCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> DeleteSelectedModsCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> OpenGitHubPageCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> OpenNexusModsPageCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> OpenSteamWorkshopPageCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> OpenSteamWorkshopPageInSteamCommand { get; }
	public ReactiveCommand<object, Unit> OpenURLCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> ToggleForceAllowInLoadOrderCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> CopyModAsDependencyCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> OpenModPropertiesCommand { get; }
	public ReactiveCommand<DivinityModData, Unit> ValidateStatsCommand { get; }

	public void OpenFile(string path)
	{
		if (File.Exists(path))
		{
			try
			{
				Process.Start(Path.GetFullPath(path));
			}
			catch (System.ComponentModel.Win32Exception) // No File Association
			{
				Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
			}
		}
		else if (Directory.Exists(path))
		{
			Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
		}
		else
		{
			DivinityApp.ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
		}
	}

	public void OpenInFileExplorer(string path)
	{
		if (!String.IsNullOrEmpty(path))
		{
			if (File.Exists(path))
			{
				Process.Start("explorer.exe", $"/select, \"{Path.GetFullPath(path)}\"");
			}
			else if (Directory.Exists(path))
			{
				Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
			}
			else
			{
				DivinityApp.ShowAlert($"Error opening '{path}': File does not exist!", AlertType.Danger, 10);
			}
		}
	}

	public void CopyToClipboard(string text)
	{
		try
		{
			if (!String.IsNullOrEmpty(text))
			{
				Clipboard.SetText(text);
				DivinityApp.ShowAlert($"Copied to clipboard: {text}", 0, 10);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
		}
	}

	public void OpenURL(string url)
	{
		if (!String.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	public void OpenGitHubPage(DivinityModData mod)
	{
		var url = mod.GetURL(ModSourceType.GITHUB);
		if (!String.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	public void OpenNexusModsPage(DivinityModData mod)
	{
		var url = mod.GetURL(ModSourceType.NEXUSMODS);
		if (!String.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	public void OpenSteamWorkshopPage(DivinityModData mod)
	{
		var url = mod.GetURL(ModSourceType.STEAM);
		if (!String.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	public void OpenSteamWorkshopPageInSteam(DivinityModData mod)
	{
		var url = mod.GetURL(ModSourceType.STEAM, true);
		if (!String.IsNullOrEmpty(url))
		{
			FileUtils.TryOpenPath(url);
		}
	}

	public void ToggleForceAllowInLoadOrder(DivinityModData mod)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			mod.ForceAllowInLoadOrder = !mod.ForceAllowInLoadOrder;
			if (mod.ForceAllowInLoadOrder)
			{
				ViewModel.AddActiveMod(mod);
			}
			else
			{
				ViewModel.RemoveActiveMod(mod);
			}
		});
	}

	public void ClearMissingMods()
	{
		_viewModel.ClearMissingMods();
	}

	public void CopyModAsDependency(DivinityModData mod)
	{
		try
		{
			var safeName = System.Security.SecurityElement.Escape(mod.Name);
			var text = String.Format(DivinityApp.XML_MODULE_SHORT_DESC_FORMATTED, mod.Folder, mod.MD5, safeName, mod.UUID, mod.Version.VersionInt);
			Clipboard.SetText(text);
			DivinityApp.ShowAlert($"Copied ModuleShortDesc for mod '{mod.Name}' to clipboard", 0, 10);
		}
		catch (Exception ex)
		{
			DivinityApp.ShowAlert($"Error copying text to clipboard: {ex}", AlertType.Danger, 10);
		}
	}

	public static void OpenModProperties(DivinityModData mod)
	{
		DivinityInteractions.OpenModProperties.Handle(mod).Subscribe();
	}

	private static CancellationTokenSource _statValidatorTokenSource;

	public static async Task ValidateModStats(DivinityModData mod, CancellationToken token)
	{
		_statValidatorTokenSource ??= new();
		var results = await ModUtils.ValidateStatsAsync([mod], Services.Settings.ManagerSettings.GameDataPath, _statValidatorTokenSource.Token);
		await DivinityInteractions.OpenValidateStatsResults.Handle(results);
	}

	private static void StartValidateModStats(DivinityModData mod)
	{
		RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, token) =>
		{
			await ValidateModStats(mod, token);
		});
	}

	public DivinityGlobalCommands()
	{
		var canExecuteViewModelCommands = this.WhenAnyValue(x => x.ViewModel, x => x.ViewModel.IsLocked, (vm, b) => vm != null && !b)
			.ObserveOn(RxApp.MainThreadScheduler);

		OpenFileCommand = ReactiveCommand.Create<string>(OpenFile, canExecuteViewModelCommands);
		OpenInFileExplorerCommand = ReactiveCommand.Create<string>(OpenInFileExplorer, canExecuteViewModelCommands);
		ClearMissingModsCommand = ReactiveCommand.Create(ClearMissingMods, canExecuteViewModelCommands);

		ToggleNameDisplayCommand = ReactiveCommand.Create<DivinityModData>((mod) =>
		{
			mod.DisplayFileForName = !mod.DisplayFileForName;
			var b = mod.DisplayFileForName;
			foreach (var m in Services.Mods.AllMods)
			{
				if (m.IsSelected)
				{
					m.DisplayFileForName = b;
				}
			}
		}, canExecuteViewModelCommands);

		CopyToClipboardCommand = ReactiveCommand.Create<string>(CopyToClipboard, canExecuteViewModelCommands);

		DeleteModCommand = ReactiveCommand.Create<DivinityModData>(mod =>
		{
			_viewModel.DeleteMod(mod);
		}, canExecuteViewModelCommands);

		DeleteSelectedModsCommand = ReactiveCommand.Create<DivinityModData>(mod =>
		{
			_viewModel.DeleteSelectedMods(mod);
		}, canExecuteViewModelCommands);

		OpenURLCommand = ReactiveCommand.Create<object>(x => OpenURL(x.ToString()), canExecuteViewModelCommands);
		OpenGitHubPageCommand = ReactiveCommand.Create<DivinityModData>(OpenGitHubPage, canExecuteViewModelCommands);
		OpenNexusModsPageCommand = ReactiveCommand.Create<DivinityModData>(OpenNexusModsPage, canExecuteViewModelCommands);
		OpenSteamWorkshopPageCommand = ReactiveCommand.Create<DivinityModData>(OpenSteamWorkshopPage, canExecuteViewModelCommands);
		OpenSteamWorkshopPageInSteamCommand = ReactiveCommand.Create<DivinityModData>(OpenSteamWorkshopPageInSteam, canExecuteViewModelCommands);
		ToggleForceAllowInLoadOrderCommand = ReactiveCommand.Create<DivinityModData>(ToggleForceAllowInLoadOrder, canExecuteViewModelCommands);
		CopyModAsDependencyCommand = ReactiveCommand.Create<DivinityModData>(CopyModAsDependency, canExecuteViewModelCommands);
		OpenModPropertiesCommand = ReactiveCommand.Create<DivinityModData>(OpenModProperties, canExecuteViewModelCommands);
		ValidateStatsCommand = ReactiveCommand.Create<DivinityModData>(StartValidateModStats, canExecuteViewModelCommands);
	}
}
