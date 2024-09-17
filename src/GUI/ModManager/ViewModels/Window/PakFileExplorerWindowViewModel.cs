using DynamicData;
using DynamicData.Binding;

using LSLib.LS;

using ModManager.Helpers;
using ModManager.Models.View;
using ModManager.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels.Window;
public class PakFileExplorerWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "pakfileexplorer";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public string Title { get; set; }
	[Reactive] public string? PakFilePath { get; set; }

	private SourceCache<PakFileEntry, string> _files = new(x => x.FilePath);
	protected SourceCache<PakFileEntry, string> Files => _files;

	private readonly ReadOnlyObservableCollection<PakFileEntry> _entries;
	public ReadOnlyObservableCollection<PakFileEntry> Entries => _entries;
	public ObservableCollectionExtended<PakFileEntry> SelectedItems { get; }
	public PakFileEntry? SelectedItem { get; set; }

	public RxCommandUnit OpenFileBrowserCommand { get; }

	private IDisposable? _loadPakTask;

	private static void AddFileToTree(string filePath, List<PakFileEntry> files, HashSet<string> directoryNames)
	{
		var directoryName = Path.GetDirectoryName(filePath);
		if (string.IsNullOrEmpty(directoryName))
		{
			files.Add(new PakFileEntry(filePath));
		}
		else
		{
			// we are adding a directory
			var firstDir = directoryName.Split(Path.DirectorySeparatorChar)[0];

			if (!directoryNames.Contains(firstDir))
			{
				files.Add(new PakFileEntry(filePath, true));
				directoryNames.Add(firstDir);
			}

			var subPath = filePath.Substring(firstDir.Length + 1);
			AddFileToTree(subPath, files, directoryNames);
		}
	}

	private async Task LoadPakAsync(IScheduler sch, CancellationToken token)
	{
		var path = PakFilePath!;

		var pr = new PackageReader();
		using var pak = pr.Read(path);

		var directories = new HashSet<string>();
		var files = new List<PakFileEntry>();

		if(pak != null && pak.Files != null)
		{
			foreach(var file in pak.Files)
			{
				if (token.IsCancellationRequested) return;

				AddFileToTree(file.Name, files, directories);
			}
		}

#if DEBUG
		DivinityApp.Log($"Directories:\n{string.Join("\n", directories)}");
		foreach(var entry in files)
		{
			entry.PrintStructure();
		}
#endif

		await Observable.Start(() =>
		{
			_files.AddOrUpdate(files);
		}, RxApp.MainThreadScheduler);
	}

	private void OnPakPathChanged(string path)
	{
		_files.Clear();

		_loadPakTask?.Dispose();
		_loadPakTask = RxApp.TaskpoolScheduler.ScheduleAsync(LoadPakAsync);
	}

	private static readonly IComparer<PakFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public PakFileExplorerWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		
		_files.Connect().ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(out _entries, _fileSort, new SortAndBindOptions { UseBinarySearch = true })
			.DisposeMany().Subscribe();

		SelectedItems = [];

		Title = "Pak File Explorer";

		var dialogService = AppServices.Get<IDialogService>();

		OpenFileBrowserCommand = ReactiveCommand.CreateFromTask(async () =>
		{
			if (dialogService != null)
			{
				var settings = AppServices.Settings;
				var pathways = AppServices.Pathways.Data;

				var dialogResult = await dialogService.OpenFileAsync(new OpenFileBrowserDialogRequest(
					"Open Pak File...",
					ModImportService.GetInitialStartingDirectory(settings.ManagerSettings.LastImportDirectoryPath),
					[CommonFileTypes.ModPak]
				));

				if(dialogResult.Success)
				{
					var filePath = dialogResult.File;
					var savedDirectory = Path.GetDirectoryName(filePath)!;
					if (settings.ManagerSettings.LastImportDirectoryPath != savedDirectory)
					{
						settings.ManagerSettings.LastImportDirectoryPath = savedDirectory;
						pathways.LastSaveFilePath = savedDirectory;
						settings.ManagerSettings.Save(out _);
					}

					PakFilePath = filePath;
				}
			}
		});

		this.WhenAnyValue(x => x.PakFilePath)
			.WhereNotNull()
			.Where(x => !String.IsNullOrEmpty(x) && File.Exists(x))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(OnPakPathChanged);
	}
}

public class DesignPakFileExplorerWindowViewModel : PakFileExplorerWindowViewModel
{
	public DesignPakFileExplorerWindowViewModel() : base()
	{
		var directory1 = new PakFileEntry("Directory1");
		for (var i = 0; i < 20; i++)
		{
			directory1.Children.Add(new PakFileEntry($"Directory1\\File_{i}"));
		}

		var directory2 = new PakFileEntry("Directory2");
		for (var i = 0; i < 20; i++)
		{
			directory2.Children.Add(new PakFileEntry($"Directory2\\File_{i}"));
		}

		Files.AddOrUpdate([directory1, directory2]);

		for (var i = 0; i < 20; i++)
		{
			Files.AddOrUpdate(new PakFileEntry($"File_{i}"));
		}
	}
}
