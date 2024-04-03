using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

using ModManager.Windows;

using SukiUI.Controls;

using System.Collections.Immutable;

namespace ModManager.Services;
public class DialogService : IDialogService
{
	private static Window _window => Locator.Current.GetService<MainWindow>()!;
	private readonly IInteractionsService _interactions;

	public async Task<OpenFileBrowserDialogResults> OpenFolderAsync(OpenFolderBrowserDialogRequest context)
	{
		var provider = _window.StorageProvider;

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.StartingPath);

		var opts = new FolderPickerOpenOptions()
		{
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			AllowMultiple = context.MultiSelect
		};

		var files = await _window.StorageProvider.OpenFolderPickerAsync(opts);

		if (files != null && files.Count > 0)
		{
			var filePaths = files.Select(x => x.TryGetLocalPath()).Where(Validators.IsValid).ToArray()!;
			return new OpenFileBrowserDialogResults(true, filePaths.FirstOrDefault(), filePaths);
		}
		return new OpenFileBrowserDialogResults();
	}

	public async Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context)
	{
		var provider = _window.StorageProvider;

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.StartingPath);

		var opts = new FilePickerOpenOptions()
		{
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			AllowMultiple = context.MultiSelect
		};

		if (context.FileTypes != null)
		{
			opts.FileTypeFilter = context.FileTypes.Select(x => x.ToFilePickerType()).ToImmutableList();
		}

		var files = await _window.StorageProvider.OpenFilePickerAsync(opts);

		if (files != null && files.Count > 0)
		{
			var filePaths = files.Select(x => x.TryGetLocalPath()).Where(Validators.IsValid).ToArray()!;
			return new OpenFileBrowserDialogResults(true, filePaths.FirstOrDefault(), filePaths);
		}
		return new OpenFileBrowserDialogResults();
	}

	public async Task<OpenFileBrowserDialogResults> SaveFileAsync(OpenFileBrowserDialogRequest context)
	{
		var provider = _window.StorageProvider;

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.StartingPath);

		var opts = new FilePickerSaveOptions()
		{
			ShowOverwritePrompt = true,
			Title = context.Title,
			SuggestedStartLocation = startingFolder
		};

		if (context.FileTypes != null)
		{
			opts.FileTypeChoices = context.FileTypes.Select(x => x.ToFilePickerType()).ToImmutableList();
			opts.DefaultExtension = context.FileTypes.FirstOrDefault().Extensions.FirstOrDefault();
		}

		var file = await _window.StorageProvider.SaveFilePickerAsync(opts);

		if (file != null)
		{
			var filePath = file.TryGetLocalPath() ?? string.Empty;
			return new OpenFileBrowserDialogResults(true, filePath, [filePath]);
		}
		return new OpenFileBrowserDialogResults();
	}

	public DialogService(IInteractionsService interactionsService)
	{
		_interactions = interactionsService;

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