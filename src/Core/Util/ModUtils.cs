using LSLib.LS;
using LSLib.LS.Stats;

using ModManager.Models.Mod;

using System.Xml;

namespace ModManager.Util;

public static class ModUtils
{
	private record struct FileText(string FilePath, string[] Lines);

	private static XmlDocument LoadXml(VFS vfs, string path)
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

	private static void LoadMod(Dictionary<string, ModInfo> mods, VFS vfs, StatLoader loader, string folderName)
	{
		if (mods.TryGetValue(folderName, out var mod))
		{
			foreach (var file in mod.Stats)
			{
				using var statStream = vfs.Open(file);
				loader.LoadStatsFromStream(file, statStream);
			}
			LoadGuidResources(vfs, loader, mod);
		}
	}

	private static readonly FileStreamOptions _defaultOpts = new()
	{
		BufferSize = 128000,
	};

	private static async Task<FileText> GetFileTextAsync(VFS vfs, string path, CancellationToken token)
	{
		var file = vfs.FindVFSFile(path);
		if (file != null)
		{
			using var stream = file.CreateContentReader();
			using var sr = new StreamReader(stream, System.Text.Encoding.UTF8, false, 128000);
			var text = await sr.ReadToEndAsync(token);
			return new FileText(path, text.Split(Environment.NewLine, StringSplitOptions.None));
		}
		return new FileText(path, []);
	}

	public static async Task<ValidateModStatsResults> ValidateStatsAsync(IEnumerable<DivinityModData> mods, string gameDataPath, CancellationToken token)
	{
		var context = new StatLoadingContext();
		var loader = new StatLoader(context);

		var vfs = new VFS();
		vfs.AttachGameDirectory(gameDataPath, true);
		foreach (var mod in mods)
		{
			if (!mod.IsEditorMod)
			{
				if (File.Exists(mod.FilePath)) vfs.AttachPackage(mod.FilePath);
			}
			else
			{
				//var publicFolder = Path.Join(gameDataPath, "Public", mod.FilePath);
				//if(Directory.Exists(publicFolder))
				//{
				//	vfs.AttachRoot(mod.FilePath);
				//}
			}
		}
		vfs.FinishBuild();

		var modResources = new ModResources();
		var modHelper = new ModPathVisitor(modResources, vfs)
		{
			Game = DivinityApp.GAME_COMPILER,
			CollectGlobals = false,
			CollectLevels = false,
			CollectStoryGoals = false,
			CollectStats = true,
			CollectGuidResources = true,
		};

		modHelper.Discover();

		var definitions = new StatDefinitionRepository();
		try
		{
			if (modResources.Mods.TryGetValue("Shared", out var shared))
			{
				definitions.LoadEnumerations(vfs.Open(shared.ValueListsFile));
				definitions.LoadDefinitions(vfs.Open(shared.ModifiersFile));
			}
			else
			{
				throw new Exception("The 'Shared' base mod appears to be missing. This is not normal.");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading definitions:\n{ex}");
		}

		context.Definitions = definitions;

		List<string> baseDependencies = ["Shared", "SharedDev", "Gustav", "GustavDev"];

		foreach (var dependency in baseDependencies)
		{
			LoadMod(modResources.Mods, vfs, loader, dependency);
		}

		var modDependencies = mods.SelectMany(x => x.Dependencies.Items.Select(x => x.Folder)).Distinct().Where(x => !baseDependencies.Contains(x));

		foreach (var dependency in modDependencies)
		{
			LoadMod(modResources.Mods, vfs, loader, dependency);
		}

		loader.ResolveUsageRef();
		loader.ValidateEntries();

		context.Errors.Clear();

		foreach (var mod in mods)
		{
			LoadMod(modResources.Mods, vfs, loader, mod.Folder);
		}

		loader.ResolveUsageRef();
		loader.ValidateEntries();

		var files = context.Errors.Select(x => x.Location?.FileName).Where(x => !String.IsNullOrEmpty(x)).ToList().Distinct().ToList();
		var textData = await Task.WhenAll(files.Select(x => GetFileTextAsync(vfs, x, token)).ToArray());
		var fileDict = textData.ToDictionary(x => x.FilePath, x => x.Lines);

		return new ValidateModStatsResults(new List<DivinityModData>(mods), context.Errors, fileDict);
	}
}
