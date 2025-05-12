using Avalonia.Platform.Storage;

using ModManager.Util;
using ModManager.Windows;

using System.Collections.Immutable;

namespace ModManager.Services;
public class DialogService : IDialogService
{
	private static Window _window => Locator.Current.GetService<MainWindow>()!;
	private readonly IInteractionsService _interactions;
	private readonly IFileSystemService _fs;
	private readonly ISettingsService _settings;
	private readonly IPathwaysService _pathways;

	public string GetInitialStartingDirectory(string? prioritizePath = null)
	{
		var directory = prioritizePath;

		if (prioritizePath.IsValid() && FileUtils.TryGetDirectoryOrParent(prioritizePath, out var actualDir))
		{
			directory = actualDir;
		}
		else
		{
			if (_settings.ManagerSettings.LastImportDirectoryPath.IsValid())
			{
				directory = _settings.ManagerSettings.LastImportDirectoryPath;
			}

			if (!_fs.Directory.Exists(directory) && !string.IsNullOrEmpty(_pathways.Data.LastSaveFilePath) && FileUtils.TryGetDirectoryOrParent(_pathways.Data.LastSaveFilePath, out var lastDir))
			{
				directory = lastDir;
			}
		}

		if (!directory.IsExistingDirectory())
		{
			directory = DivinityApp.GetAppDirectory();
		}

		return directory;
	}

	public async Task<OpenFileBrowserDialogResults> OpenFolderAsync(OpenFolderBrowserDialogRequest context)
	{
		IStorageProvider? provider = null;

		if (context.TargetWindow is Window window)
		{
			provider = window.StorageProvider;
		}
		else
		{
			provider = _window.StorageProvider;
		}

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.SuggestedDirectory);

		var opts = new FolderPickerOpenOptions()
		{
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			SuggestedFileName = context.SuggestedName,
			AllowMultiple = context.MultiSelect
		};

		var files = await provider.OpenFolderPickerAsync(opts);

		if (files != null && files.Count > 0)
		{
			var filePaths = files.Select(x => x.TryGetLocalPath()).Where(Validators.IsValid).ToArray()!;
			return new OpenFileBrowserDialogResults(true, filePaths.FirstOrDefault(), filePaths);
		}
		return new OpenFileBrowserDialogResults();
	}

	public async Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context)
	{
		IStorageProvider? provider = null;

		if(context.TargetWindow is Window window)
		{
			provider = window.StorageProvider;
		}
		else
		{
			provider = _window.StorageProvider;
		}

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.SuggestedDirectory);

		var opts = new FilePickerOpenOptions()
		{
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			SuggestedFileName = context.SuggestedName,
			AllowMultiple = context.MultiSelect
		};

		if (context.FileTypes != null)
		{
			opts.FileTypeFilter = context.FileTypes.Select(x => x.ToFilePickerType()).ToImmutableList();
		}

		var files = await provider.OpenFilePickerAsync(opts);

		if (files != null && files.Count > 0)
		{
			var filePaths = files.Select(x => x.TryGetLocalPath()).Where(Validators.IsValid).ToArray()!;
			return new OpenFileBrowserDialogResults(true, filePaths.FirstOrDefault(), filePaths);
		}
		return new OpenFileBrowserDialogResults();
	}

	public async Task<OpenFileBrowserDialogResults> SaveFileAsync(OpenFileBrowserDialogRequest context)
	{
		IStorageProvider? provider = null;

		if (context.TargetWindow is Window window)
		{
			provider = window.StorageProvider;
		}
		else
		{
			provider = _window.StorageProvider;
		}

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.SuggestedDirectory);

		var opts = new FilePickerSaveOptions()
		{
			ShowOverwritePrompt = true,
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			SuggestedFileName = context.SuggestedName,
		};

		if (context.FileTypes != null)
		{
			opts.FileTypeChoices = context.FileTypes.Select(x => x.ToFilePickerType()).ToImmutableList();
			opts.DefaultExtension = context.FileTypes.FirstOrDefault().Extensions.FirstOrDefault();
		}

		var file = await provider.SaveFilePickerAsync(opts);

		if (file != null)
		{
			var filePath = file.TryGetLocalPath() ?? string.Empty;
			return new OpenFileBrowserDialogResults(true, filePath, [filePath]);
		}
		return new OpenFileBrowserDialogResults();
	}

	public DialogService(IInteractionsService interactionsService, IFileSystemService fileSystemService, ISettingsService settings, IPathwaysService pathways)
	{
		_interactions = interactionsService;
		_fs = fileSystemService;
		_settings = settings;
		_pathways = pathways;

		_interactions.OpenFileBrowserDialog.RegisterHandler(context =>
		{
			return Observable.StartAsync(async () =>
			{
				return await OpenFileAsync(context.Input);
			}, RxApp.MainThreadScheduler);
		});

		_interactions.OpenFolderBrowserDialog.RegisterHandler(context =>
		{
			return Observable.StartAsync(async () =>
			{
				return await OpenFolderAsync(context.Input);
			}, RxApp.MainThreadScheduler);
		});
		_pathways = pathways;
	}
}
/* // Fluent Avalonia
			var td = new TaskDialog
			{
				Content = data.Message,
				SubHeader = data.Title,
			};

			if (data.MessageBoxType.HasFlag(InteractionMessageBoxType.Confirmation))
			{
				td.Buttons = [TaskDialogButton.YesButton, TaskDialogButton.CancelButton];
			}
			else
			{
				td.Buttons = [TaskDialogButton.OKButton];
			}

			if (data.MessageBoxType.HasFlag(InteractionMessageBoxType.Error))
			{
				td.IconSource = new SymbolIconSource { Symbol = Symbol.StopFilled };
			}

			var app = App.Current.ApplicationLifetime;

			if (app is IClassicDesktopStyleApplicationLifetime desktop)
			{
				td.XamlRoot = desktop.MainWindow;
			}
			else if (app is ISingleViewApplicationLifetime single)
			{
				td.XamlRoot = TopLevel.GetTopLevel(single.MainView);
			}

			var result = await td.ShowAsync(true);

			if (result is TaskDialogStandardResult taskResult)
			{
				switch (taskResult)
				{
					case TaskDialogStandardResult.OK:
					case TaskDialogStandardResult.Yes:
						context.SetOutput(true);
						return;
					default:
						context.SetOutput(false);
						break;
				}
			} 
*/