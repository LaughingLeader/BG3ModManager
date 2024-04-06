using LSLib.LS;

using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Services;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ModManager.Util.Pak;

public partial class DirectoryPakParser(string directoryPath, EnumerationOptions? opts = null, 
	Dictionary<string, DivinityModData>? baseMods = null, HashSet<string>? packageBlackList = null) : IDisposable
{
	private bool _isDisposed;
	private readonly List<Package> _packages = [];
	private readonly IFileSystemService _fs = Locator.Current.GetService<IFileSystemService>()!;
	private readonly IEnvironmentService _environment = Locator.Current.GetService<IEnvironmentService>()!;
	private readonly EnumerationOptions _opts = opts ?? FileUtils.FlatSearchOptions;
	private readonly Dictionary<string, DivinityModData> _baseMods = baseMods ?? [];
	private readonly HashSet<string> _packageBlackList = packageBlackList ?? PackageBlacklistBG3;

	public string DirectoryPath { get; } = directoryPath;
	public List<DivinityModData> Mods { get; } = [];

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
		"Game.pak",
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

	private static bool CanProcessPak(string path, HashSet<string> packageBlacklist)
	{
		var baseName = Path.GetFileName(path);
		if (!packageBlacklist.Contains(baseName)
			// Don't load 2nd, 3rd, ... parts of a multi-part archive
			&& !ModPathVisitor.archivePartRe.IsMatch(baseName))
		{
			return true;
		}
		return false;
	}
	#endregion

	private async Task<ModDirectoryLoadingResults> LoadPackagesAsync(bool detectDuplicates, bool parseLooseMetaFiles, CancellationToken token)
	{
		
		var opts = new ParallelOptions()
		{
			CancellationToken = token,
			MaxDegreeOfParallelism = _environment.ProcessorCount
		};

		ConcurrentDictionary<string, DivinityModData> loadedMods = [];
		ConcurrentBag<DivinityModData> dupes = [];

		await Parallel.ForEachAsync(_packages, opts, async (package, t) =>
		{
			var parsed = await DivinityModDataLoader.LoadModDataFromPakAsync(package, _baseMods, t, !detectDuplicates);
			if (parsed?.Count > 0)
			{
				foreach(var mod in parsed)
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
		});


		if (parseLooseMetaFiles)
		{
			var metaFiles = Directory.EnumerateFiles(directoryPath, "meta.lsx", FileUtils.RecursiveOptions);
			await Parallel.ForEachAsync(metaFiles, opts, async (metaFilePath, t) =>
			{
				var mod = await DivinityModDataLoader.GetModDataFromMeta(metaFilePath, t);
				if(mod != null)
				{
					mod.IsEditorMod = true;
					mod.FilePath = metaFilePath;

					try
					{
						mod.LastModified = _fs.File.GetLastWriteTime(metaFilePath);
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error getting last modified date for '{mod.FilePath}': {ex}");
					}

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
			});
		}

		return new ModDirectoryLoadingResults(DirectoryPath)
		{
			Mods = new(loadedMods),
			Duplicates = new(dupes)
		};
	}

	private void ProcessPartitionPakPath(IEnumerator<string> partition)
	{
		using (partition)
		{
			while (partition.MoveNext())
			{
				var reader = new PackageReader();
				var package = reader.Read(partition.Current);
				_packages.Add(package);
			}
		}
	}

	public async Task<ModDirectoryLoadingResults> ProcessAsync(bool detectDuplicates, bool parseLooseMetaFiles, CancellationToken token)
	{
#if DEBUG
		if(!_fs.Directory.Exists(directoryPath)) throw new DirectoryNotFoundException(directoryPath);
#endif
		if (Directory.Exists(directoryPath))
		{
			var time = DateTimeOffset.Now;

			var files = Directory.EnumerateFiles(directoryPath, "*.pak", _opts).Where(x => CanProcessPak(x, _packageBlackList));

			Partitioner.Create(files)
				.GetPartitions(_environment.ProcessorCount)
				.AsParallel()
				.ForAll(ProcessPartitionPakPath);

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
				_packages?.ForEach(x => x.Dispose());
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
