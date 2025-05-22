using Avalonia.Controls.Models.TreeDataGrid;

using DynamicData;
using DynamicData.Binding;

using LSLib.LS;

using ModManager.Helpers;
using ModManager.Models.Mod;
using ModManager.Models.View;
using ModManager.Services;
using ModManager.Windows;

using System.Collections.Concurrent;

namespace ModManager.ViewModels.Window;
public class PakFileExplorerWindowViewModel : BaseProgressViewModel, IClosableViewModel
{
	private readonly IDialogService _dialogService;
	private readonly IGlobalCommandsService _commands;
	private readonly IFileSystemService _fs;

	[Reactive] public string Title { get; set; }

	private readonly SourceCache<ModFileEntry, string> _files = new(x => x.FilePath);
	protected SourceCache<ModFileEntry, string> Files => _files;
	public HierarchicalTreeDataGridSource<ModFileEntry> FileTreeSource { get; }

	public ObservableCollectionExtended<ModFileEntry> SelectedItems { get; }
	public ModFileEntry? SelectedItem { get; set; }

	public RxCommandUnit OpenFileBrowserCommand { get; }
	public ReactiveCommand<object?, Unit> CopyToClipboardCommand { get; }
	public ReactiveCommand<ModFileEntry, Unit> ExtractPakFilesCommand { get; }

	private void AddFileToTree(PackagedFileInfo pakFile, ConcurrentDictionary<string, ModFileEntry> directories, CancellationToken token) => AddFileToTree(pakFile.Name, directories, token, pakFile.UncompressedSize);

	private void AddFileToTree(string filePath, ConcurrentDictionary<string, ModFileEntry> directories, CancellationToken token, double? fileSize = null)
	{
		var immediateParentDirectory = Path.GetDirectoryName(filePath);

		ModFileEntry? parentDirectory = null;

		if (!string.IsNullOrEmpty(immediateParentDirectory))
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
					if (token.IsCancellationRequested) break;

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
							parentDirectory = new ModFileEntry(nextFullDirPath, true);
							directories.TryAdd(nextFullDirPath, parentDirectory);
						}
					}
					else
					{
						if (parentDirectory.TryGetChild(nextFullDirPath, out var subDirectory))
						{
							parentDirectory = subDirectory;
						}
						else
						{
							subDirectory = new ModFileEntry(nextFullDirPath, true);
							parentDirectory.AddChild(subDirectory);
							parentDirectory = subDirectory;
						}
					}
				}
			}
		}

		if (parentDirectory != null)
		{
			parentDirectory.AddChild(new ModFileEntry(filePath, false, fileSize ?? _fs.FileInfo.New(filePath).Length));
		}
		else
		{
			directories.TryAdd(filePath, new ModFileEntry(filePath));
		}
	}

	private async Task LoadPakAsync(string pakPath, CancellationToken token)
	{
		DivinityApp.Log($"Loading pak... {pakPath}");

		var pr = new PackageReader();
		using var pak = pr.Read(pakPath);

		var directories = new ConcurrentDictionary<string, ModFileEntry>();

		DivinityApp.Log("Building file tree");

		foreach(var file in pak.Files)
		{
			AddFileToTree(file, directories, token);
		}

		DivinityApp.Log("Finished parsing pak files. Adding to tree.");

		await Observable.Start(() =>
		{
			_files.AddOrUpdate(directories.Values);
		}, RxApp.MainThreadScheduler);
	}

	private async Task LoadLooseDataAsync(ModData mod, CancellationToken token)
	{
		var modFolder = mod.Folder;
		var gameDirectory = _fs.Directory.GetParent(mod.FilePath).Parent.Parent.FullName;

		var modsFolder = _fs.Path.Join(gameDirectory, "Mods", modFolder);
		var publicFolder = _fs.Path.Join(gameDirectory, "Public", modFolder);
		var projectFolder = _fs.Directory.GetParent(mod.ToolkitProjectMeta.FilePath).FullName;
		var editorModsFolder = _fs.Path.Join(gameDirectory, "Editor", "Mods", modFolder);
		var generatedPublicFolder = _fs.Path.Join(gameDirectory, "Generated", "Public", modFolder);

		List<string> sourceDirs = [modsFolder, publicFolder, projectFolder, editorModsFolder, generatedPublicFolder];
		var directories = new ConcurrentDictionary<string, ModFileEntry>();

		DivinityApp.Log($"Loading loose data containing mod folder... {modFolder}");

		ConcurrentBag<string> files = [];

		foreach(var dir in sourceDirs)
		{
			if (token.IsCancellationRequested) break;
			if(dir.IsExistingDirectory())
			{
				foreach(var file in _fs.Directory.EnumerateFiles(dir))
				{
					AddFileToTree(file, directories, token);
				}
			}
		}

		DivinityApp.Log("Finished parsing loose files. Adding to tree.");

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

	private static void BuildFileHash(ref HashSet<string> output, ModFileEntry file)
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

	private async Task ExtractPakFilesAsync(IEnumerable<ModFileEntry> pakFiles, CancellationToken token)
	{
		var settings = AppServices.Settings;
		var pathways = AppServices.Pathways.Data;

		var dialogResult = await _dialogService.OpenFolderAsync(new OpenFolderBrowserDialogRequest(
			"Extract File(s) To...",
			_dialogService.GetInitialStartingDirectory(settings.ManagerSettings.LastExtractOutputPath),
			null,
			null,
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
					await using var outStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 32000, FileOptions.Asynchronous);
					await inStream.CopyToAsync(outStream, 32000, token);
				});
			}
		}
	}

	private static readonly IComparer<ModFileEntry> _fileSort = new NaturalFileSortComparer(StringComparison.OrdinalIgnoreCase);

	public async Task LoadMods(IEnumerable<ModData> mods, CancellationToken token)
	{
		var pakLoadingTasks = new List<Task>();
		var looseLoadingTasks = new List<Task>();
		foreach(var mod in mods)
		{
			if (token.IsCancellationRequested) break;
			if(mod.FilePath.IsExistingFile())
			{
				if (!mod.IsLooseMod)
				{
					pakLoadingTasks.Add(LoadPakAsync(mod.FilePath, token));
				}
				else if (mod.FilePath.IsExistingFile())
				{
					looseLoadingTasks.Add(LoadLooseDataAsync(mod, token));
				}
			}
		}
		await Task.WhenAll(pakLoadingTasks);
		await Task.WhenAll(looseLoadingTasks);
	}

	public PakFileExplorerWindowViewModel()
	{
		_commands ??= AppServices.Commands;
		_fs ??= AppServices.FS;
		_dialogService ??= AppServices.Dialog;

		ObservableCollectionExtended<ModFileEntry> readOnlyFiles = [];
		Files.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(readOnlyFiles, _fileSort, new SortAndBindOptions { UseBinarySearch = true })
			.DisposeMany()
			.Subscribe();

		FileTreeSource = new HierarchicalTreeDataGridSource<ModFileEntry>(readOnlyFiles)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<ModFileEntry>(
					//new TextColumn<PakFileEntry, string>("Name", x => x.FileName, GridLength.Star),
					new TemplateColumn<ModFileEntry>("Name", "FileNameWithIconCell", null, GridLength.Star),
					x => x.Subfiles, x => x.Subfiles != null && x.Subfiles.Count > 0, x => x.IsExpanded),
				new TextColumn<ModFileEntry, string>("Size", x => x.Size, GridLength.Auto),
			},
		};

		SelectedItems = [];

		FileTreeSource.RowSelection!.SelectionChanged += (o, e) =>
		{
			SelectedItems.Clear();
			if (FileTreeSource.RowSelection!.SelectedItems != null && FileTreeSource.RowSelection!.SelectedItems.Count > 0)
			{
				SelectedItems.AddRange(FileTreeSource.RowSelection!.SelectedItems);
			}
			SelectedItem = FileTreeSource.RowSelection!.SelectedItem;
		};

		Title = "Pak File Explorer";

		if (_commands != null)
		{
			CopyToClipboardCommand = _commands.CopyToClipboardCommand;
		}
		else
		{
			CopyToClipboardCommand = ReactiveCommand.Create<object?>(str => { });
		}

		OpenFileBrowserCommand = ReactiveCommand.CreateFromTask(async () =>
		{
			if (_dialogService != null)
			{
				var settings = AppServices.Settings;
				var pathways = AppServices.Pathways.Data;

				var dialogResult = await _dialogService.OpenFileAsync(new OpenFileBrowserDialogRequest(
					"Open Pak File...",
					_dialogService.GetInitialStartingDirectory(settings.ManagerSettings.LastImportDirectoryPath),
					[CommonFileTypes.ModPak],
					window: AppServices.Get<PakFileExplorerWindow>()
				));

				if (dialogResult.Success)
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

		ExtractPakFilesCommand = ReactiveCommand.Create<ModFileEntry>(pakFile =>
		{
			_extractPakTask?.Dispose();
			var files = SelectedItems.ToList();
			if (files.Count > 0)
			{
				_extractPakTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
				{
					await ExtractPakFilesAsync(files, t);
				});
			}
		}, hasFilesSelected);

		this.WhenAnyValue(x => x.PakFilePath)
			.WhereNotNull()
			.Where(Validators.IsExistingFile)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(OnPakPathChanged);
	}

	[DependencyInjectionConstructor]
	public PakFileExplorerWindowViewModel(IDialogService dialogService, IGlobalCommandsService commands, IFileSystemService fs) : this()
	{
		_dialogService = dialogService;
		_commands = commands;
		_fs = fs;
	}
}

public class DesignPakFileExplorerWindowViewModel : PakFileExplorerWindowViewModel
{
	public DesignPakFileExplorerWindowViewModel() : base()
	{
		var random = new Random();

		var directory1 = new ModFileEntry("Directory1");
		for (var i = 0; i < 20; i++)
		{
			directory1.AddChild(new ModFileEntry($"Directory1\\File_{i}", false, random.NextDouble() * (random.NextInt64(1024, 6464) ^ 2)));
		}

		var directory2 = new ModFileEntry("Directory2");
		for (var i = 0; i < 20; i++)
		{
			directory2.AddChild(new ModFileEntry($"Directory2\\File_{i}", false, random.NextDouble() * (random.NextInt64(1024, 6464) ^ 2)));
		}

		Files.AddOrUpdate([directory1, directory2]);

		for (var i = 0; i < 20; i++)
		{
			Files.AddOrUpdate(new ModFileEntry($"File_{i}", false, random.NextDouble() * (random.NextInt64(1024, 6464) ^ 2)));
		}
	}
}
