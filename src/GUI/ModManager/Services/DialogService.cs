using Avalonia.Controls;
using Avalonia.Platform.Storage;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Services;
public class DialogService : IDialogService
{
	private readonly Window _window;
	private readonly IInteractionsService _interactions;

	public async Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context)
	{
		var provider = _window.StorageProvider;

		var startingFolder = await provider.TryGetFolderFromPathAsync(context.StartingPath);

		var opts = new FilePickerOpenOptions()
		{
			Title = context.Title,
			SuggestedStartLocation = startingFolder,
			AllowMultiple = context.MultiSelect,
		};

		if (context.FileTypes != null)
		{
			opts.FileTypeFilter = context.FileTypes.Select(x =>x.ToFilePickerType()).ToImmutableList();
		}

		var files = await _window.StorageProvider.OpenFilePickerAsync(opts);

		if(files != null && files.Count > 0)
		{
			string[] filePaths = files.Select(x => x.TryGetLocalPath()).Where(string.IsNullOrEmpty).ToArray()!;
			return new OpenFileBrowserDialogResults(true, filePaths.FirstOrDefault(), filePaths, files.Count == 1);
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
			return new OpenFileBrowserDialogResults(true, filePath, [filePath], true);
		}
		return new OpenFileBrowserDialogResults();
	}

	public DialogService(Window targetWindow)
	{
		_window = targetWindow;
		_interactions = Locator.Current.GetService<IInteractionsService>()!;

		_interactions.OpenFileBrowserDialog.RegisterHandler(context =>
		{
			return Observable.StartAsync(async () =>
			{
				return await OpenFileAsync(context.Input);
			}, RxApp.MainThreadScheduler);
		});
	}
}
