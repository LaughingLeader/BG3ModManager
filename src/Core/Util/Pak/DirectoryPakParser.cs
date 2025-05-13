using LSLib.LS;

using ModManager.Extensions;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.Mod.Game;
using ModManager.Services;

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace ModManager.Util.Pak;

public partial class DirectoryPakParser(string directoryPath, EnumerationOptions? opts = null,
	IDictionary<string, ModData>? baseMods = null, HashSet<string>? packageBlackList = null) : IDisposable
{
	private bool _isDisposed;
	private readonly ConcurrentBag<Package> _packages = [];
	private readonly IFileSystemService _fs = Locator.Current.GetService<IFileSystemService>()!;
	private readonly IEnvironmentService _environment = Locator.Current.GetService<IEnvironmentService>()!;
	private readonly EnumerationOptions _opts = opts ?? FileUtils.FlatSearchOptions;
	private readonly IDictionary<string, ModData> _baseMods = baseMods ?? new Dictionary<string, ModData>();
	private readonly HashSet<string> _packageBlackList = packageBlackList ?? PackageBlacklistBG3;

	public string DirectoryPath => directoryPath;
	public List<ModData> Mods { get; } = [];

	#region Static Properties

	[GeneratedRegex("^(.*)_[0-9]+\\.pak$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex ArchivePartRegex();

	// Pattern for excluding subsequent parts of a multi-part archive
	private static readonly Regex _archivePartPattern = ArchivePartRegex();

	private static readonly EnumerationOptions _gameDataFolderOptions = new()
	{
		RecurseSubdirectories = true,
		IgnoreInaccessible = true,
		MaxRecursionDepth = 1,
		MatchCasing = MatchCasing.CaseInsensitive
	};

	private static readonly EnumerationOptions _flatEnumerationOptions = new()
	{
		RecurseSubdirectories = false,
		IgnoreInaccessible = true,
		MatchCasing = MatchCasing.CaseInsensitive
	};

	//Packages to ignore in DOS2 use the same names here (Textures.pak etc)
	public static readonly HashSet<string> PackageBlacklistBG3 = [
		"Assets.pak",
		"Effects.pak",
		"Engine.pak",
		"EngineShaders.pak",
		//"Game.pak",
		"GamePlatform.pak",
		"Gustav_NavCloud.pak",
		"Gustav_Textures.pak",
		"Gustav_Video.pak",
		"Icons.pak",
		"LowTex.pak",
		"Materials.pak",
		"Minimaps.pak",
		"Models.pak",
		"PsoCache.pak",
		"SharedSoundBanks.pak",
		"SharedSounds.pak",
		"Textures.pak",
		"VirtualTextures.pak",
        // Localization
        "English.pak",
		"English_Animations.pak",
		"VoiceMeta.pak",
		"Voice.pak"
	];

	private bool CanProcessPak(string path, HashSet<string> packageBlacklist)
	{
		var baseName = _fs.Path.GetFileName(path);
		if (!packageBlacklist.Contains(baseName)
			// Don't load 2nd, 3rd, ... parts of a multi-part archive
			&& !ModPathVisitor.archivePartRe.IsMatch(baseName))
		{
			return true;
		}
		return false;
	}
	#endregion

	private static async Task LoadToolkitResourcesAsync(string metaPath, ConcurrentDictionary<string, ToolkitProjectMetaData> output, CancellationToken token)
	{
		var resource = await ModDataLoader.LoadResourceAsync(metaPath, LSLib.LS.Enums.ResourceFormat.LSX, token);
		if(resource != null)
		{
			var toolkitMeta = ToolkitProjectMetaData.FromResource(resource);
			if (toolkitMeta.Module.IsValid())
			{
				toolkitMeta.FilePath = metaPath;
				output.TryAdd(toolkitMeta.Module, toolkitMeta);
			}
		}
	}

	private async Task<ModData?> LoadModsMetaResourcesAsync(string metaFilePath, ConcurrentDictionary<string, ToolkitProjectMetaData> toolkitProjects, CancellationToken token)
	{
		var mod = await ModDataLoader.GetModDataFromMeta(metaFilePath, token);
		if (mod != null)
		{
			mod.IsLooseMod = true;
			mod.FilePath = metaFilePath;
			if (toolkitProjects.TryGetValue(mod.UUID, out var toolkitData))
			{
				mod.IsToolkitProject = true;
				mod.ToolkitProjectMeta = toolkitData;
			}

			var parentFolder = _fs.Path.GetDirectoryName(metaFilePath)!;
			await ModDataLoader.TryLoadConfigFilesFromPath(_fs, parentFolder, mod, token);

			try
			{
				mod.LastModified = _fs.File.GetLastWriteTime(metaFilePath);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error getting last modified date for '{mod.FilePath}': {ex}");
			}

			return mod;
		}
		return null;
	}

	private async Task<ModDirectoryLoadingResults> LoadPackagesAsync(bool detectDuplicates, bool parseLooseMetaFiles, CancellationToken token)
	{
		ConcurrentDictionary<string, ModData> loadedMods = [];
		ConcurrentBag<ModData> dupes = [];

		var packageLoadTasks = new List<Task<List<ModData>?>>();
		foreach (var package in _packages)
		{
			if (token.IsCancellationRequested) break;
			packageLoadTasks.Add(ModDataLoader.LoadModDataFromPakAsync(package, _baseMods, token, !detectDuplicates));
		}
		var parsedResults = await Task.WhenAll(packageLoadTasks);

		if (parsedResults.Length > 0)
		{
			foreach(var modList in parsedResults)
			{
				if(modList != null)
				{
					foreach (var mod in modList)
					{
						if (detectDuplicates)
						{
							if (loadedMods.ContainsKey(mod.UUID))
							{
								dupes.Add(mod);
							}
							else
							{
								loadedMods[mod.UUID] = mod;
							}
						}
						else
						{
							loadedMods[mod.UUID] = mod;
						}
					}
				}
			}
		}

		if (parseLooseMetaFiles)
		{
			var modsMetaDirectory = _fs.Path.Join(directoryPath, "Mods");
			var projectsMetaDirectory = _fs.Path.Join(directoryPath, "Projects");

			var toolkitProjects = new ConcurrentDictionary<string, ToolkitProjectMetaData>();

			if (projectsMetaDirectory.IsExistingDirectory())
			{
				var toolkitMetaFiles = _fs.Directory.EnumerateFiles(projectsMetaDirectory, "meta.lsx", FileUtils.RecursiveOptions);

				var toolkitProjectTasks = new List<Task>();
				foreach (var toolkitMetaPath in toolkitMetaFiles)
				{
					if (token.IsCancellationRequested) break;
					toolkitProjectTasks.Add(LoadToolkitResourcesAsync(toolkitMetaPath, toolkitProjects, token));
				}
				await Task.WhenAll(toolkitProjectTasks);
			}

			if(modsMetaDirectory.IsExistingDirectory())
			{
				var metaFiles = _fs.Directory.EnumerateFiles(modsMetaDirectory, "meta.lsx", FileUtils.RecursiveOptions);

				var metaTasks = new List<Task<ModData?>>();
				foreach (var metaPath in metaFiles)
				{
					if (token.IsCancellationRequested) break;
					metaTasks.Add(LoadModsMetaResourcesAsync(metaPath, toolkitProjects, token));
				}
				var looseMods = await Task.WhenAll(metaTasks);

				foreach(var looseMod in looseMods)
				{
					if(looseMod != null)
					{
						if (detectDuplicates)
						{
							if (loadedMods.ContainsKey(looseMod.UUID))
							{
								dupes.Add(looseMod);
							}
							else
							{
								loadedMods.TryAdd(looseMod.UUID, looseMod);
							}
						}
						else
						{
							loadedMods.TryAdd(looseMod.UUID, looseMod);
						}
					}
				}
			}
		}

		return new ModDirectoryLoadingResults(DirectoryPath)
		{
			Mods = new(loadedMods),
			Duplicates = [.. dupes]
		};
	}

	public async Task<ModDirectoryLoadingResults> ProcessAsync(bool detectDuplicates, bool parseLooseMetaFiles, CancellationToken token)
	{
#if DEBUG
		if (!_fs.Directory.Exists(directoryPath)) throw new DirectoryNotFoundException(directoryPath);
#endif
		if (_fs.Directory.Exists(directoryPath))
		{
			var time = DateTimeOffset.Now;

			var files = _fs.Directory.EnumerateFiles(directoryPath, "*.pak", _opts).Where(x => CanProcessPak(x, _packageBlackList));

			foreach (var pak in files)
			{
				if (token.IsCancellationRequested) break;
				var reader = new PackageReader();
				var package = reader.Read(pak);
				if (package != null)
				{
					_packages.Add(package);
				}
			}

			var timeTaken = $"{DateTimeOffset.Now - time:s\\.ff}";
			System.Diagnostics.Trace.WriteLine($"Took {timeTaken} second(s) to enumerate files");

			return await LoadPackagesAsync(detectDuplicates, parseLooseMetaFiles, token);
		}
		return new(DirectoryPath);
	}

	#region IDiposable

	protected virtual void Dispose(bool disposing)
	{
		if (!_isDisposed)
		{
			if (disposing)
			{
				if(_packages != null )
				{
					foreach(var package in _packages)
					{
						package?.Dispose();
					}
				}
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_isDisposed = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~DirectoryPakParser()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	#endregion
}
