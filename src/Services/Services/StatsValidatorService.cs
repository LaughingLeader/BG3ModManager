using LSLib.LS;
using LSLib.Stats;

using ModManager.Models.Mod;

using System.IO;
using System.Text;
using System.Xml;

namespace ModManager.Services;
public class StatsValidatorService : IStatsValidatorService
{
	private readonly IFileSystemService _fs;

	private readonly StatDefinitionRepository _definitions;
	private readonly StatLoadingContext _context;
	private readonly StatLoader _loader;
	private readonly VFS _vfs;
	private readonly ModResources _modResources;
	private readonly ModPathVisitor _modHelper;

#if !DOS2
	private readonly List<string> _baseDependencies = ["Shared", "SharedDev", "Gustav", "GustavDev"];
#else
	private readonly List<string> _baseDependencies = ["Shared", "Origins"];
#endif

	private string? _gameDataPath;

	public string? GameDataPath => _gameDataPath;

	private record struct FileText(string FilePath, string[] Lines);

	private static XmlDocument? LoadXml(VFS vfs, string path)
	{
		if (path == null) return null;

		using var stream = vfs.Open(path);

		var doc = new XmlDocument();
		doc.Load(stream);
		return doc;
	}

	private static void LoadGuidResources(VFS vfs, StatLoader loader, ModInfo mod)
	{
		var actionResources = LoadXml(vfs, mod.ActionResourcesFile);
		if (actionResources != null)
		{
			loader.LoadActionResources(actionResources);
		}

		var actionResourceGroups = LoadXml(vfs, mod.ActionResourceGroupsFile);
		if (actionResourceGroups != null)
		{
			loader.LoadActionResourceGroups(actionResourceGroups);
		}
	}

	private static bool LoadMod(Dictionary<string, ModInfo> mods, VFS vfs, StatLoader loader, string folderName)
	{
		if (mods.TryGetValue(folderName, out var mod))
		{
			foreach (var file in mod.Stats)
			{
				using var statStream = vfs.Open(file);
				loader.LoadStatsFromStream(file, statStream);
			}
			LoadGuidResources(vfs, loader, mod);
			return true;
		}
		return false;
	}

	private static readonly FileStreamOptions _defaultOpts = new()
	{
		BufferSize = 128000,
	};

	private async Task<FileText> GetFileTextAsync(VFS vfs, string path, string gameDataPath, CancellationToken token)
	{
		if(vfs.TryOpen(path, out var stream))
		{
			using var sr = new StreamReader(stream, Encoding.UTF8, false, 128000);
			var text = await sr.ReadToEndAsync(token);
			await stream.DisposeAsync();
			return new FileText(path, text.Split(Environment.NewLine, StringSplitOptions.None));
		}
		return new FileText(path, []);
	}

	public void Initialize(string gameDataPath)
	{
		_vfs.AttachGameDirectory(gameDataPath, true);
		_gameDataPath = gameDataPath;
		_vfs.FinishBuild();

		_modHelper.Discover();

		try
		{
			if (_modResources.Mods.TryGetValue("Shared", out var shared))
			{
				_definitions.LoadEnumerations(_vfs.Open(shared.ValueListsFile));
				_definitions.LoadDefinitions(_vfs.Open(shared.ModifiersFile));
			}
			else
			{
				throw new Exception("The 'Shared' base mod appears to be missing. This is not normal.");
			}
			_definitions.LoadLSLibDefinitionsEmbedded();
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading definitions:\n{ex}");
		}

		foreach (var dependency in _baseDependencies)
		{
			LoadMod(_modResources.Mods, _vfs, _loader, dependency);
		}
	}

	public async Task<ValidateModStatsResults> ValidateModsAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		var validationVFS = new VFS(_vfs, true); // true so it doesn't clone Packages

		var time = DateTimeOffset.Now;

		_modHelper.FS = validationVFS;

		foreach (var mod in mods)
		{
			if (!mod.IsEditorMod)
			{
				if (_fs.File.Exists(mod.FilePath))
				{
					validationVFS.AttachPackage(mod.FilePath);
				}
			}
			else if(_gameDataPath != null && !mod.FilePath.Contains(_gameDataPath))
			{
				var publicFolder = _fs.Path.Join(_gameDataPath, "Public", mod.FilePath);
				if (_fs.Directory.Exists(publicFolder))
				{
					validationVFS.AttachRoot(mod.FilePath);
				}
			}
		}

		validationVFS.FinishBuild();

		_modHelper.Discover();

		var loadErrors = new List<string>();
		var context = new StatLoadingContext(_context);
		var loader = new StatLoader(context);

		var modDependencies = mods.SelectMany(x => x.Dependencies.Items.Select(x => x.Folder)).Distinct().Where(x => !_baseDependencies.Contains(x));

		foreach (var dependency in modDependencies)
		{
			if (!LoadMod(_modResources.Mods, validationVFS, loader, dependency))
			{
				loadErrors.Add($"Dependency mod '{dependency}' not found.");
			}
		}

		loader.ResolveUsageRef();
		loader.ValidateEntries();

		context.Errors.Clear();

		foreach (var mod in mods)
		{
			if (!LoadMod(_modResources.Mods, validationVFS, loader, mod.Folder))
			{
				loadErrors.Add($"Mod '{mod.Name}' not found.");
			}
		}

		loader.ResolveUsageRef();
		loader.ValidateEntries();

		List<string> files = context.Errors.Select(x => x.Location?.FileName).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList()!;
		var textData = await Task.WhenAll(files.Select(x => GetFileTextAsync(validationVFS, x, _gameDataPath, token)).ToArray());
		var fileDict = textData.ToDictionary(x => x.FilePath, x => x.Lines);

		_modHelper.FS = _vfs;

		return new ValidateModStatsResults([..mods], context.Errors, fileDict, DateTimeOffset.Now - time);
	}

	public StatsValidatorService(IFileSystemService fileSystemService)
	{
		_fs = fileSystemService;

		_definitions = new StatDefinitionRepository();
		_context = new StatLoadingContext(_definitions);
		_loader = new StatLoader(_context);

		_vfs = new VFS();

		_modResources = new ModResources();
		_modHelper = new ModPathVisitor(_modResources, _vfs)
		{
			Game = DivinityApp.GAME_COMPILER,
			CollectGlobals = false,
			CollectLevels = false,
			CollectStoryGoals = false,
			CollectStats = true,
			CollectGuidResources = true,
		};
	}
}
