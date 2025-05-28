using LSLib.LS;
using LSLib.Stats;

using ModManager.Extensions;
using ModManager.Models.Mod;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace ModManager.Util;

public static partial class ModUtils
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

	public static async Task<ValidateModStatsResults> ValidateStatsAsync(IEnumerable<ModData> mods, string gameDataPath, CancellationToken token)
	{
		var definitions = new StatDefinitionRepository();
		var context = new StatLoadingContext(definitions);
		var loader = new StatLoader(context);

		var time = DateTimeOffset.Now;

		var vfs = new VFS();
		vfs.AttachGameDirectory(gameDataPath, true);
		foreach (var mod in mods)
		{
			if (!mod.IsLooseMod)
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
			definitions.LoadLSLibDefinitionsEmbedded();
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

		var files = context.Errors.Select(x => x.Location?.FileName).Where(Validators.IsValid).ToList().Distinct().ToList();
		var textData = await Task.WhenAll(files.Select(x => GetFileTextAsync(vfs, x, token)).ToArray());
		var fileDict = textData.ToDictionary(x => x.FilePath, x => x.Lines);

		return new ValidateModStatsResults([.. mods], context.Errors, fileDict, DateTimeOffset.Now - time);
	}

	[GeneratedRegex("@([^\\s]+?)([\\s]+)([^@\\s]*)")]
	private static partial Regex FilterPropertyRe();
	[GeneratedRegex("@([^\\s]+?)([\\s\"]+)([^@\"]*)")]
	private static partial Regex FilterPropertyPatternWithQuotesRe();

	private static readonly Regex s_filterPropertyPattern = FilterPropertyRe();
	private static readonly Regex s_filterPropertyPatternWithQuotes = FilterPropertyPatternWithQuotesRe();

	public static void FilterMods(string? filterText, IEnumerable<IModEntry> mods)
	{
		foreach (var m in mods)
		{
			m.IsHidden = false;
		}

		if (!string.IsNullOrWhiteSpace(filterText))
		{
			if (filterText.IndexOf('@') > -1)
			{
				var remainingSearch = filterText;
				List<ModFilterData> searchProps = [];

				MatchCollection matches;

				if (filterText.IndexOf('\"') > -1)
				{
					matches = s_filterPropertyPatternWithQuotes.Matches(filterText);
				}
				else
				{
					matches = s_filterPropertyPattern.Matches(filterText);
				}

				if (matches.Count > 0)
				{
					foreach (var match in matches.Cast<Match>())
					{
						if (match.Success)
						{
							var prop = match.Groups[1]?.Value;
							var value = match.Groups[3]?.Value;
							if (!value.IsValid()) value = "";
							if (!string.IsNullOrWhiteSpace(prop))
							{
								searchProps.Add(new ModFilterData()
								{
									FilterProperty = prop,
									FilterValue = value
								});

								remainingSearch = remainingSearch.Replace(match.Value, "");
							}
						}
					}
				}

				remainingSearch = remainingSearch.Replace("\"", "");

				//If no Name property is specified, use the remaining unmatched text for that
				if (remainingSearch.IsValid() && !searchProps.Any(f => f.PropertyContains("Name")))
				{
					remainingSearch = remainingSearch.Trim();
					searchProps.Add(new ModFilterData()
					{
						FilterProperty = "Name",
						FilterValue = remainingSearch
					});
				}

				foreach (var mod in mods)
				{
					//@Mode GM @Author Leader
					var totalMatches = 0;
					foreach (var f in searchProps)
					{
						if (f.Match(mod))
						{
							totalMatches += 1;
						}
					}

					if (totalMatches < searchProps.Count)
					{
						mod.IsHidden = true;
					}
				}
			}
			else
			{
				foreach (var m in mods)
				{
					if (m.DisplayName.IsValid())
					{
						var matchIndex = CultureInfo.CurrentCulture.CompareInfo.IndexOf(m.DisplayName, filterText, CompareOptions.IgnoreCase);

						if (matchIndex <= -1)
						{
							m.IsHidden = true;
						}
					}
				}
			}
		}
	}

	public static void FilterMods(string? filterText, IEnumerable<ModData> mods)
	{
		foreach (var m in mods)
		{
			m.IsHidden = false;
		}

		if (!string.IsNullOrWhiteSpace(filterText))
		{
			if (filterText.IndexOf('@') > -1)
			{
				var remainingSearch = filterText;
				List<ModFilterData> searchProps = [];

				MatchCollection matches;

				if (filterText.IndexOf('\"') > -1)
				{
					matches = s_filterPropertyPatternWithQuotes.Matches(filterText);
				}
				else
				{
					matches = s_filterPropertyPattern.Matches(filterText);
				}

				if (matches.Count > 0)
				{
					foreach (var match in matches.Cast<Match>())
					{
						if (match.Success)
						{
							var prop = match.Groups[1]?.Value;
							var value = match.Groups[3]?.Value;
							if (!value.IsValid()) value = "";
							if (!string.IsNullOrWhiteSpace(prop))
							{
								searchProps.Add(new ModFilterData()
								{
									FilterProperty = prop,
									FilterValue = value
								});

								remainingSearch = remainingSearch.Replace(match.Value, "");
							}
						}
					}
				}

				remainingSearch = remainingSearch.Replace("\"", "");

				//If no Name property is specified, use the remaining unmatched text for that
				if (remainingSearch.IsValid() && !searchProps.Any(f => f.PropertyContains("Name")))
				{
					remainingSearch = remainingSearch.Trim();
					searchProps.Add(new ModFilterData()
					{
						FilterProperty = "Name",
						FilterValue = remainingSearch
					});
				}

				foreach (var mod in mods)
				{
					//@Mode GM @Author Leader
					var totalMatches = 0;
					foreach (var f in searchProps)
					{
						if (f.Match(mod))
						{
							totalMatches += 1;
						}
					}

					if (totalMatches < searchProps.Count)
					{
						mod.IsHidden = true;
					}
				}
			}
			else
			{
				foreach (var m in mods)
				{
					if (m.DisplayName.IsValid())
					{
						var matchIndex = CultureInfo.CurrentCulture.CompareInfo.IndexOf(m.DisplayName, filterText, CompareOptions.IgnoreCase);

						if (matchIndex <= -1)
						{
							m.IsHidden = true;
						}
					}
				}
			}
		}
	}
}
