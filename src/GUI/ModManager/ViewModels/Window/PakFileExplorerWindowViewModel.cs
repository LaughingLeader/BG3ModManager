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
public class PakFileExplorerWindowViewModel : ReactiveObject, IClosableViewModel
{
	#region IClosableViewModel
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

	private static void AddFileToTree(string filePath, Dictionary<string, PakFileEntry> directories)
	{
		var immediateParentDirectory = Path.GetDirectoryName(filePath);

		PakFileEntry? parentDirectory = null;

		if(!string.IsNullOrEmpty(immediateParentDirectory))
		{
			immediateParentDirectory = immediateParentDirectory.Replace(Path.DirectorySeparatorChar, '/');

			DivinityApp.Log($"[{filePath}] immediateParentDirectory({immediateParentDirectory})");

			if (directories.TryGetValue(immediateParentDirectory, out var parent))
			{
				parentDirectory = parent;
			}
			else
			{
				var fileDirs = immediateParentDirectory.Split('/');

				DivinityApp.Log($"fileDirs({string.Join(';', fileDirs)})");

				var nextFullDirPath = "";
				foreach (var dir in fileDirs)
				{
					if (nextFullDirPath != "") nextFullDirPath += "/";
					nextFullDirPath += dir;

					DivinityApp.Log($"nextFullDirPath({nextFullDirPath})");

					if (parentDirectory == null)
					{
						if (directories.TryGetValue(nextFullDirPath, out var nextDir))
						{
							parentDirectory = nextDir;
						}
						else
						{
							parentDirectory = new PakFileEntry(nextFullDirPath, true);
							DivinityApp.Log($"Adding directory ({nextFullDirPath}) to root");
							directories.Add(nextFullDirPath, parentDirectory);
						}
					}
					else
					{
						var subDirectory = parentDirectory.Children.Cast<PakFileEntry>().FirstOrDefault(x => x.FilePath == nextFullDirPath);
						if (subDirectory != null)
						{
							parentDirectory = subDirectory;
						}
						else
						{
							subDirectory = new PakFileEntry(nextFullDirPath, true);
							DivinityApp.Log($"Adding directory ({nextFullDirPath}) to ({parentDirectory.FileName})");
							parentDirectory.Children.Add(subDirectory);
							parentDirectory = subDirectory;
						}
					}
				}
			}
		}

		if(parentDirectory != null)
		{
			DivinityApp.Log($"Adding file({filePath}) to ({parentDirectory.FilePath})");
			parentDirectory.Children.Add(new PakFileEntry(filePath));
		}
		else
		{
			DivinityApp.Log($"Adding file({filePath}) to root");
			directories.Add(filePath, new PakFileEntry(filePath));
		}
	}

	private async Task LoadPakAsync(IScheduler sch, CancellationToken token)
	{
		var path = PakFilePath!;

		var pr = new PackageReader();
		using var pak = pr.Read(path);

		var directories = new Dictionary<string, PakFileEntry>();

		if(pak != null && pak.Files != null)
		{
			foreach(var file in pak.Files)
			{
				if (token.IsCancellationRequested) return;
				AddFileToTree(file.Name, directories);
			}
		}

#if DEBUG
		DivinityApp.Log($"Directories:\n{string.Join("\n", directories)}");
		foreach(var entry in directories.Values)
		{
			entry.PrintStructure();
		}
#endif

		await Observable.Start(() =>
		{
			_files.AddOrUpdate(directories.Values);
		}, RxApp.MainThreadScheduler);
	}

	private void OnPakPathChanged(string path)
	{
		_files.Clear();

		_loadPakTask?.Dispose();
		_loadPakTask = RxApp.TaskpoolScheduler.ScheduleAsync(LoadPakAsync);
	}

	private static readonly IComparer<PakFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public PakFileExplorerWindowViewModel()
	{
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
