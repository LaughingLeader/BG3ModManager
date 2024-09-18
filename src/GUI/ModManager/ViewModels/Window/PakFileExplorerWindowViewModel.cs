using DynamicData;
using DynamicData.Binding;

using LSLib.LS;

using ModManager.Helpers;
using ModManager.Models.View;
using ModManager.Services;
using ModManager.Windows;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.ViewModels.Window;
public class PakFileExplorerWindowViewModel : BaseProgressViewModel, IClosableViewModel
{
	private readonly IDialogService _dialogService;

	[Reactive] public string Title { get; set; }
	[Reactive] public string? PakFilePath { get; set; }

	private SourceCache<PakFileEntry, string> _files = new(x => x.FilePath);
	protected SourceCache<PakFileEntry, string> Files => _files;

	private readonly ReadOnlyObservableCollection<PakFileEntry> _entries;
	public ReadOnlyObservableCollection<PakFileEntry> Entries => _entries;
	public ObservableCollectionExtended<PakFileEntry> SelectedItems { get; }
	public PakFileEntry? SelectedItem { get; set; }

	public RxCommandUnit OpenFileBrowserCommand { get; }
	public ReactiveCommand<string?, Unit> CopyToClipboardCommand { get; }
	public ReactiveCommand<PakFileEntry, Unit> ExtractPakFilesCommand { get; }

	private static void AddFileToTree(string filePath, ConcurrentDictionary<string, PakFileEntry> directories)
	{
		var immediateParentDirectory = Path.GetDirectoryName(filePath);

		PakFileEntry? parentDirectory = null;

		if(!string.IsNullOrEmpty(immediateParentDirectory))
		{
			immediateParentDirectory = immediateParentDirectory.Replace(Path.DirectorySeparatorChar, '/');

			if (directories.TryGetValue(immediateParentDirectory, out var parent))
			{
				parentDirectory = parent;
			}
			else
			{
				var fileDirs = immediateParentDirectory.Split('/');

				var nextFullDirPath = "";
				foreach (var dir in fileDirs)
				{
					if (nextFullDirPath != "") nextFullDirPath += "/";
					nextFullDirPath += dir;

					if (parentDirectory == null)
					{
						if (directories.TryGetValue(nextFullDirPath, out var nextDir))
						{
							parentDirectory = nextDir;
						}
						else
						{
							parentDirectory = new PakFileEntry(nextFullDirPath, true);
							directories.TryAdd(nextFullDirPath, parentDirectory);
						}
					}
					else
					{
						if(parentDirectory.TryGetChild(nextFullDirPath, out var subDirectory))
						{
							parentDirectory = subDirectory;
						}
						else
						{
							subDirectory = new PakFileEntry(nextFullDirPath, true);
							parentDirectory.AddChild(subDirectory);
							parentDirectory = subDirectory;
						}
					}
				}
			}
		}

		if(parentDirectory != null)
		{
			parentDirectory.AddChild(new PakFileEntry(filePath));
		}
		else
		{
			directories.TryAdd(filePath, new PakFileEntry(filePath));
		}
	}

	private async Task LoadPakAsync(CancellationToken token)
	{
		var path = PakFilePath!;

		DivinityApp.Log($"Loading pak... {path}");

		var pr = new PackageReader();
		using var pak = pr.Read(path);

		var directories = new ConcurrentDictionary<string, PakFileEntry>();

		DivinityApp.Log("Building file tree");

		var opts = new ParallelOptions()
		{
			CancellationToken = token,
			MaxDegreeOfParallelism = AppServices.Get<IEnvironmentService>().ProcessorCount
		};

		await Parallel.ForEachAsync(pak.Files, opts, async (file, t) =>
		{
			if (t.IsCancellationRequested) return;
			//await Task.Run(() => AddFileToTree(file.Name, directories), t);
			try
			{
				AddFileToTree(file.Name, directories);
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"{ex}");
			}
		});

		DivinityApp.Log("Finished parsing pak files. Adding to tree.");

		await Observable.Start(() =>
		{
			_files.AddOrUpdate(directories.Values);
		}, RxApp.MainThreadScheduler);
	}

	private IDisposable? _loadPakTask;
	private IDisposable? _extractPakTask;

	private void OnPakPathChanged(string path)
	{
		_loadPakTask?.Dispose();

		_files.Clear();

		if(File.Exists(path))
		{
			_loadPakTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
			{
				await LoadPakAsync(t);
			});
		}
	}

	private static void BuildFileHash(ref HashSet<string> output, PakFileEntry file)
	{
		if(file.IsDirectory)
		{
			foreach(var child in file.Subfiles)
			{
				BuildFileHash(ref output, child);
			}
		}
		else
		{
			output.Add(file.FilePath);
		}
	}

	private async Task ExtractPakFilesAsync(IEnumerable<PakFileEntry> pakFiles, CancellationToken token)
	{
		var settings = AppServices.Settings;
		var pathways = AppServices.Pathways.Data;

		var dialogResult = await _dialogService.OpenFolderAsync(new OpenFolderBrowserDialogRequest(
			"Extract File(s) To...",
			ModImportService.GetInitialStartingDirectory(settings.ManagerSettings.LastExtractOutputPath),
			"",
			AppServices.Get<PakFileExplorerWindow>()
		));

		if (dialogResult.Success)
		{
			var extractToDirectory = dialogResult.File!;

			if (settings.ManagerSettings.LastExtractOutputPath != extractToDirectory)
			{
				settings.ManagerSettings.LastExtractOutputPath = extractToDirectory;
				pathways.LastSaveFilePath = extractToDirectory;
				settings.ManagerSettings.Save(out _);
			}

			if (File.Exists(PakFilePath))
			{
				var fileMap = new HashSet<string>();

				foreach (var file in pakFiles)
				{
					BuildFileHash(ref fileMap, file);
				}

				var pr = new PackageReader();
				using var pak = pr.Read(PakFilePath);

				var files = new ConcurrentBag<PackagedFileInfo>();
				foreach(var file in pak.Files)
				{
					if (token.IsCancellationRequested) return;

					if (file.IsDeletion()) continue;

					if (fileMap.Contains(file.Name))
					{
						files.Add(file);
					}
				}

				var opts = new ParallelOptions()
				{
					CancellationToken = token,
					MaxDegreeOfParallelism = AppServices.Get<IEnvironmentService>().ProcessorCount
				};

				await Parallel.ForEachAsync(files, opts, async (f, t) =>
				{
					var outputPath = Path.Combine(extractToDirectory, f.Name);
					if(Path.GetDirectoryName(outputPath) is string parentFolder)
					{
						Directory.CreateDirectory(parentFolder);
					}
					using var inStream = f.CreateContentReader();
					using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 32000, true);
					await inStream.CopyToAsync(outStream, 32000, token);
				});
			}
		}
	}

	private static readonly IComparer<PakFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public PakFileExplorerWindowViewModel(IDialogService? dialogService = null, IGlobalCommandsService? commands = null)
	{
		_dialogService = dialogService ?? AppServices.Get<IDialogService>();
		commands ??= AppServices.Get<IGlobalCommandsService>();

		_files.Connect().ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(out _entries, _fileSort, new SortAndBindOptions { UseBinarySearch = true })
			.DisposeMany().Subscribe();

		SelectedItems = [];

		Title = "Pak File Explorer";

		if(commands != null)
		{
			CopyToClipboardCommand = commands.CopyToClipboardCommand;
		}
		else
		{
			CopyToClipboardCommand = ReactiveCommand.Create<string?>(str => { });
		}

		OpenFileBrowserCommand = ReactiveCommand.CreateFromTask(async () =>
		{
			if (dialogService != null)
			{
				var settings = AppServices.Settings;
				var pathways = AppServices.Pathways.Data;

				var dialogResult = await dialogService.OpenFileAsync(new OpenFileBrowserDialogRequest(
					"Open Pak File...",
					ModImportService.GetInitialStartingDirectory(settings.ManagerSettings.LastImportDirectoryPath),
					[CommonFileTypes.ModPak],
					window: AppServices.Get<PakFileExplorerWindow>()
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

		var hasFilesSelected = SelectedItems.ToObservableChangeSet().CountChanged().Select(x => SelectedItems.Count > 0).ObserveOn(RxApp.MainThreadScheduler);

		ExtractPakFilesCommand = ReactiveCommand.Create<PakFileEntry>(pakFile =>
		{
			_extractPakTask?.Dispose();
			var files = SelectedItems.ToList();
			if(files.Count > 0)
			{
				_extractPakTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
				{
					await ExtractPakFilesAsync(files, t);
				});
			}
		}, hasFilesSelected);

		this.WhenAnyValue(x => x.PakFilePath)
			.WhereNotNull()
			.Where(x => !string.IsNullOrEmpty(x) && File.Exists(x))
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
			directory1.AddChild(new PakFileEntry($"Directory1\\File_{i}"));
		}

		var directory2 = new PakFileEntry("Directory2");
		for (var i = 0; i < 20; i++)
		{
			directory2.AddChild(new PakFileEntry($"Directory2\\File_{i}"));
		}

		Files.AddOrUpdate([directory1, directory2]);

		for (var i = 0; i < 20; i++)
		{
			Files.AddOrUpdate(new PakFileEntry($"File_{i}"));
		}
	}
}
