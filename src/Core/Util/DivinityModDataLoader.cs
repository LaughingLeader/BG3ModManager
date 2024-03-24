using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Models.Mod;

using DynamicData;

using LSLib.LS;
using LSLib.LS.Enums;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DivinityModManager.Util;

public static partial class DivinityModDataLoader
{
	private static readonly StringComparison SCOMP = StringComparison.OrdinalIgnoreCase;
	private static readonly string[] LarianFileTypes = [".lsb", ".lsf", ".lsx", ".lsj"];
				  
	public static readonly ulong HEADER_MAJOR = 4;
	public static readonly ulong HEADER_MINOR = 6;
	public static readonly ulong HEADER_REVISION = 0;
	public static readonly ulong HEADER_BUILD = 1;

	private static readonly string[] VersionAttributes = ["Version64", "Version"];

	private static readonly string[] _IgnoredRecursiveFolders = [
		"Baldur's Gate 3\\Data",
		"Baldur's Gate 3\\bin",
		"Localization"
	];

	public static readonly HashSet<string> IgnoreBuiltinPath = [];
	private static readonly HashSet<string> _allPaksNames = [];

	private static readonly ResourceLoadParameters _loadParams = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);

	[GeneratedRegex("^(Mods|Public|Generated)/(.+?)/.+$")]
	private static partial Regex ModFolderPattern();

	[GeneratedRegex("(_[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
	private static partial Regex MultiPartPakPattern();

	[GeneratedRegex("^Mods/([^/]+)/meta.lsx", RegexOptions.IgnoreCase)]
	private static partial Regex ModMetaPattern();

	[GeneratedRegex("value=\"(.*?)\"")]
	private static partial Regex XamlValuePattern();

	private static readonly Regex _multiPartPakPatternNoExtension = MultiPartPakPattern();
	private static readonly Regex _modMetaPattern = ModMetaPattern();
	private static readonly Regex _ModFolderPattern = ModFolderPattern();

	public static bool IgnoreMod(string modUUID)
	{
		return DivinityApp.IgnoredMods.Any(m => m.UUID == modUUID);
	}

	public static bool IgnoreModDependency(string modUUID)
	{
		return DivinityApp.IgnoredDependencyMods.Any(m => m.UUID == modUUID) || IgnoreMod(modUUID);
	}

	public static bool IgnoreModByFolder(string folder)
	{
		return DivinityApp.IgnoredMods.Any(m => m.Folder.Equals(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), SCOMP));
	}

	public static string MakeSafeFilename(string filename, char replaceChar)
	{
		foreach (char c in System.IO.Path.GetInvalidFileNameChars())
		{
			filename = filename.Replace(c, replaceChar);
		}
		return filename;
	}

	/// <summary>
	/// Gets an attribute node with the supplied id, return the value.
	/// </summary>
	/// <param name="node"></param>
	/// <param name="attribute"></param>
	/// <param name="fallbackValue"></param>
	/// <returns></returns>
	private static string GetAttributeWithId(XElement node, string id, string fallbackValue = "")
	{
		var att = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
		if (att != null)
		{
			return att;
		}
		return fallbackValue;
	}

	private static string GetAttributeWithId(XElement node, string[] ids, string fallbackValue = "")
	{
		foreach (var id in ids)
		{
			var att = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
			if (att != null)
			{
				return att;
			}
		}
		return fallbackValue;
	}

	private static bool TryGetAttribute(XElement node, string id, out string value, string fallbackValue = "")
	{
		var att = node.Attributes().FirstOrDefault(a => a.Name == id);
		if (att != null)
		{
			value = att.Value;
			return true;
		}
		value = fallbackValue;
		return false;
	}

	private static ulong SafeConvertStringUnsigned(string str)
	{
		if (!String.IsNullOrWhiteSpace(str) && UInt64.TryParse(str, out ulong val))
		{
			return val;
		}
		return 0UL;
	}

	public static string EscapeXml(string s)
	{
		string toxml = s;
		if (!String.IsNullOrEmpty(toxml))
		{
			// replace literal values with entities
			toxml = toxml.Replace("&", "&amp;");
			toxml = toxml.Replace("'", "&apos;");
			toxml = toxml.Replace("\"", "&quot;");
			toxml = toxml.Replace(">", "&gt;");
			toxml = toxml.Replace("<", "&lt;");
		}
		return toxml;
	}

	public static string EscapeXmlAttributes(string xmlstring)
	{
		if (!String.IsNullOrEmpty(xmlstring))
		{
			xmlstring = XamlValuePattern().Replace(xmlstring, new MatchEvaluator((m) =>
			{
				return $"value=\"{EscapeXml(m.Groups[1].Value)}\"";
			}));
		}
		return xmlstring;
	}

	public static string UnescapeXml(string str)
	{
		if (!String.IsNullOrEmpty(str))
		{
			str = str.Replace("&amp;", "&");
			str = str.Replace("&apos;", "'");
			str = str.Replace("&quot;", "\"");
			str = str.Replace("&gt;", ">");
			str = str.Replace("&lt;", "<");
			str = str.Replace("<br>", Environment.NewLine);
		}
		return str;
	}

	private static DivinityModData ParseMetaFile(string metaContents)
	{
		try
		{
			XElement xDoc = XElement.Parse(EscapeXmlAttributes(metaContents));
			var versionNode = xDoc.Descendants("version").FirstOrDefault();

			var headerMajor = HEADER_MAJOR;
			var headerMinor = HEADER_MINOR;
			var headerRevision = HEADER_REVISION;
			var headerBuild = HEADER_BUILD;

			if (versionNode != null)
			{
				//DivinityApp.LogMessage($"Version node: {versionNode.ToString()}");
				//DOS2 Classic Mods <version major="3" minor="1" revision="3" build="5" />
				//DE Mods <version major="3" minor="6" revision="2" build="0" />
				//BG3 Mods <version major="4" minor="0" revision="9" build="331"/>
				if (TryGetAttribute(versionNode, "major", out var headerMajorStr))
				{
					ulong.TryParse(headerMajorStr, out headerMajor);
				}
				if (TryGetAttribute(versionNode, "minor", out var headerMinorStr))
				{
					ulong.TryParse(headerMinorStr, out headerMinor);
				}
				if (TryGetAttribute(versionNode, "revision", out var headerRevisionStr))
				{
					ulong.TryParse(headerRevisionStr, out headerRevision);
				}
				if (TryGetAttribute(versionNode, "build", out var headerBuildStr))
				{
					ulong.TryParse(headerBuildStr, out headerBuild);
				}

				//DivinityApp.LogMessage($"Version: {headerMajor}.{headerMinor}.{headerRevision}.{headerBuild}");
			}

			var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
			if (moduleInfoNode != null)
			{
				var uuid = GetAttributeWithId(moduleInfoNode, "UUID", "");
				var name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Name", ""));
				var description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Description", ""));
				var author = UnescapeXml(GetAttributeWithId(moduleInfoNode, "Author", ""));
				/*
				if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
				{
					name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
					description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
					author = "Larian Studios";
				}
				*/
				DivinityModData modData = new()
				{
					HasMetadata = true,
					UUID = uuid,
					Name = name,
					Author = author,
					Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(moduleInfoNode, VersionAttributes, ""))),
					Folder = GetAttributeWithId(moduleInfoNode, "Folder", ""),
					Description = description,
					MD5 = GetAttributeWithId(moduleInfoNode, "MD5", ""),
					ModType = GetAttributeWithId(moduleInfoNode, "Type", ""),
					HeaderVersion = new LarianVersion(headerMajor, headerMinor, headerRevision, headerBuild)
				};

				var tagsText = GetAttributeWithId(moduleInfoNode, "Tags", "");
				if (!String.IsNullOrWhiteSpace(tagsText))
				{
					modData.AddTags(tagsText.Split(';'));
				}
				//var dependenciesNodes = xDoc.SelectNodes("//node[@id='ModuleShortDesc']");
				var dependenciesNodes = xDoc.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

				if (dependenciesNodes != null)
				{
					foreach (var node in dependenciesNodes)
					{
						DivinityModDependencyData dependencyMod = new()
						{
							UUID = GetAttributeWithId(node, "UUID", ""),
							Name = UnescapeXml(GetAttributeWithId(node, "Name", "")),
							Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(node, VersionAttributes, ""))),
							Folder = GetAttributeWithId(node, "Folder", ""),
							MD5 = GetAttributeWithId(node, "MD5", "")
						};
						//DivinityApp.LogMessage($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
						if (dependencyMod.UUID != "")
						{
							modData.Dependencies.Add(dependencyMod);
						}
					}
				}

				var publishVersionNode = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "PublishVersion").FirstOrDefault();
				if (publishVersionNode != null)
				{
					var publishVersion = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(publishVersionNode, VersionAttributes, "")));
					modData.PublishVersion = publishVersion;
					//DivinityApp.LogMessage($"{modData.Folder} PublishVersion is {publishVersion.Version}");
				}

				var targets = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "Target");
				if (targets != null)
				{
					foreach (var node in targets)
					{
						var target = GetAttributeWithId(node, "Object", "");
						if (!String.IsNullOrWhiteSpace(target))
						{
							modData.Modes.Add(target);
						}
					}
					modData.Targets = String.Join(", ", modData.Modes);
				}
				return modData;
			}
			else
			{
				DivinityApp.Log($"**[ERROR] ModuleInfo node not found for meta.lsx: {metaContents}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error parsing meta.lsx: {ex}");
		}
		return null;
	}

	//BOM
	private static readonly string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());

	private static System.IO.FileStream GetAsyncStream(string filePath)
	{
		return new System.IO.FileStream(filePath,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				2048,
				FileOptions.Asynchronous);
	}

	private static async Task TryLoadConfigFiles(VFS vfs, DivinityModData modData, string modsFolder, CancellationToken token)
	{
		Stream extenderConfigStream = null;
		Stream modManagerConfigStream = null;

		try
		{
			var extenderConfigPath = Path.Join(modsFolder, DivinityApp.EXTENDER_MOD_CONFIG);
			if (vfs.TryOpen(extenderConfigPath, out extenderConfigStream))
			{
				var extenderConfig = await LoadScriptExtenderConfigAsync(extenderConfigStream);

				if (extenderConfig != null)
				{
					modData.ScriptExtenderData = extenderConfig;
					if (modData.ScriptExtenderData.RequiredVersion > -1) modData.HasScriptExtenderSettings = true;
				}
				else
				{
					DivinityApp.Log($"Failed to parse {DivinityApp.EXTENDER_MOD_CONFIG} for '{modsFolder}'.");
				}
			}

			var modManagerConfigPath = Path.Join(modsFolder, ModConfig.FileName);
			if (vfs.TryOpen(modManagerConfigPath, out modManagerConfigStream))
			{
				var modManagerConfig = await DivinityJsonUtils.DeserializeFromAbstractAsync<ModConfig>(modManagerConfigStream, token);
				if (modManagerConfig != null)
				{
					modData.ApplyModConfig(modManagerConfig);
				}
			}
		}
		finally
		{
			extenderConfigStream?.Dispose();
			modManagerConfigStream?.Dispose();
		}
	}

	private static bool PakIsNotPartial(string path)
	{
		var baseName = Path.GetFileNameWithoutExtension(path);
		var match = _multiPartPakPatternNoExtension.Match(baseName);
		if (match.Success)
		{
			var nameWithoutPartial = baseName.Replace(match.Groups[0].Value, "");
			if (_allPaksNames.Contains(nameWithoutPartial))
			{
				DivinityApp.Log($"Pak ({baseName}) is a partial pak for ({nameWithoutPartial}). Skipping.");
				return false;
			}
		}
		return true;
	}
	
	private static bool IsModMetaFile(PackagedFileInfo f)
	{
		if (Path.GetFileName(f.Name).Equals("meta.lsx", SCOMP))
		{
			return _modMetaPattern.IsMatch(f.Name);
		}
		return false;
	}

	private static async Task<DivinityModData> InternalLoadModDataFromPakAsync(Package pak, string pakPath, Dictionary<string, DivinityModData> builtinMods, CancellationToken token)
	{
		DivinityModData modData = null;

		string pakName = Path.GetFileNameWithoutExtension(pakPath);

		var metaFiles = new List<PackagedFileInfo>();
		var hasBuiltinDirectory = false;
		var isOverridingBuiltinDirectory = false;
		var hasModFolderData = false;
		var hasOsirisScripts = DivinityOsirisModStatus.NONE;
		var builtinModOverrides = new Dictionary<string, DivinityModData>();
		var files = new HashSet<string>();
		var baseGameFiles = new HashSet<string>();

		PackagedFileInfo extenderConfigPath = null;
		PackagedFileInfo modManagerConfigPath = null;

		if (pak != null && pak.Files != null)
		{
			for (int i = 0; i < pak.Files.Count; i++)
			{
				if (token.IsCancellationRequested) break;
				var f = pak.Files[i];
				files.Add(f.Name);

				var formattedPath = f.Name.Replace("/", @"\");

				if (formattedPath.EndsWith(DivinityApp.EXTENDER_MOD_CONFIG, StringComparison.OrdinalIgnoreCase))
				{
					extenderConfigPath = f;
				}
				else if (formattedPath.EndsWith(ModConfig.FileName, StringComparison.OrdinalIgnoreCase) && Path.GetDirectoryName(formattedPath) == "Mods")
				{
					modManagerConfigPath = f;
				}
				else if (IsModMetaFile(f))
				{
					metaFiles.Add(f);
				}
				else
				{
					var modFolderMatch = _ModFolderPattern.Match(formattedPath);
					if (modFolderMatch.Success)
					{
						var modFolder = Path.GetFileName(modFolderMatch.Groups[2].Value.TrimEnd(Path.DirectorySeparatorChar));
						if (formattedPath.EndsWith($"Mods\\{modFolder}\\{ModConfig.FileName}", StringComparison.OrdinalIgnoreCase))
						{
							modManagerConfigPath = f;
						}
						else if (formattedPath.Contains($"Mods\\{modFolder}\\Story\\RawFiles\\Goals", StringComparison.OrdinalIgnoreCase))
						{
							if (hasOsirisScripts == DivinityOsirisModStatus.NONE)
							{
								hasOsirisScripts = DivinityOsirisModStatus.SCRIPTS;
							}
							if (formattedPath.EndsWith("ForceRecompile.txt", StringComparison.OrdinalIgnoreCase))
							{
								hasOsirisScripts = DivinityOsirisModStatus.MODFIXER;
							}
							else
							{
								using var stream = f.CreateContentReader();
								using var sr = new StreamReader(stream);
								string text = await sr.ReadToEndAsync();
								if (text.Contains("NRD_KillStory") || text.Contains("NRD_BadCall"))
								{
									hasOsirisScripts = DivinityOsirisModStatus.MODFIXER;
								}
							}
						}

						if (builtinMods.TryGetValue(modFolder, out var builtinMod))
						{
							hasBuiltinDirectory = true;
							if (!IgnoreBuiltinPath.Any(x => f.Name.Contains(x)))
							{
								isOverridingBuiltinDirectory = true;

								if (f.Size() > 0)
								{
									if (modFolder == "Game" && f.Name.Contains("GUI"))
									{
										if (f.Name.EndsWith(".xaml")) baseGameFiles.Add(f.Name);
									}
									else
									{
										baseGameFiles.Add(f.Name);
									}
								}

								if (!builtinModOverrides.ContainsKey(modFolder))
								{
									builtinModOverrides[builtinMod.Folder] = builtinMod;
									DivinityApp.Log($"Found a mod with a builtin directory. Pak({pakName}) Folder({modFolder}) File({f.Name})");
								}
							}
						}
						else
						{
							hasModFolderData = true;
						}
					}
				}
			}
		}

		var metaCount = metaFiles.Count;
		PackagedFileInfo metaFile = null;

		if (metaCount == 0)
		{
			// Assume it's an override mod since it doesn't have a meta file.
			isOverridingBuiltinDirectory = hasBuiltinDirectory;
		}
		else
		{
			for (int i = 0; i < metaCount; i++)
			{
				var f = metaFiles[i];
				if (metaFile == null)
				{
					metaFile = f;
				}
				else
				{
					var parentDir = Directory.GetParent(f.Name);
					// A pak may have multiple meta.lsx files for overriding NumPlayers or something. Match against the pak name in that case.
					if (pakName.Contains(parentDir.Name))
					{
						metaFile = f;
						break;
					}
				}
			}
		}

		if (metaFile != null)
		{
			//DivinityApp.LogMessage($"Parsing meta.lsx for mod pak '{pakPath}'.");
			using var stream = metaFile.CreateContentReader();
			using var sr = new System.IO.StreamReader(stream);
			var text = await sr.ReadToEndAsync();
			modData = ParseMetaFile(text);
			if (modData != null && isOverridingBuiltinDirectory)
			{
				modData.IsForceLoadedMergedMod = hasModFolderData;
			}
		}
		else if (isOverridingBuiltinDirectory)
		{
			//var pakData = new DivinityPakFile()
			//{
			//	FilePath = pakPath,
			//	BuiltinOverrideModsText = String.Join(Environment.NewLine, builtinModOverrides.Values.OrderBy(x => x.Name).Select(x => $"{x.Folder} ({x.Name})"))
			//};
			//overridePaks.Add(pakData);
			modData = new DivinityModData()
			{
				UUID = pakName,
				FilePath = pakPath,
				Name = pakName,
				Folder = builtinModOverrides.FirstOrDefault().Key,
				Description = "This file overrides base game data.",
				ModType = "File Override",
			};
			DivinityApp.Log($"Adding a file override mod pak '{modData.Name}'.");
		}

		if (modData != null)
		{
			modData.OsirisModStatus = hasOsirisScripts;
			modData.Files = files;
			if (isOverridingBuiltinDirectory)
			{
				if (baseGameFiles.Count > 0 && baseGameFiles.Count < DivinityApp.MAX_FILE_OVERRIDE_DISPLAY)
				{
					modData.BuiltinOverrideModsText = String.Join(Environment.NewLine, baseGameFiles.OrderBy(x => x));
				}
				else
				{
					modData.BuiltinOverrideModsText = String.Join(Environment.NewLine, builtinModOverrides.Values.OrderBy(x => x.Name).Select(x => $"{x.Folder} ({x.Name})"));
				}
				modData.IsForceLoaded = true;
			}
			modData.FilePath = pakPath;
			if (File.Exists(pakPath))
			{
				try
				{
					modData.LastModified = File.GetLastWriteTime(pakPath);
					modData.LastUpdated = modData.LastModified;
				}
				catch (PlatformNotSupportedException ex)
				{
					DivinityApp.Log($"Error getting pak last modified date for '{pakPath}': {ex}");
				}
			}

			modData.IsUserMod = true;

			if (extenderConfigPath != null)
			{
				var extenderConfig = await LoadScriptExtenderConfigAsync(extenderConfigPath);
				if (extenderConfig != null)
				{
					modData.ScriptExtenderData = extenderConfig;
					if (modData.ScriptExtenderData.RequiredVersion > -1) modData.HasScriptExtenderSettings = true;
				}
				else
				{
					DivinityApp.Log($"Failed to parse {extenderConfigPath} for '{pakPath}'.");
				}
			}

			if (modManagerConfigPath != null)
			{
				var modManagerConfig = await DivinityJsonUtils.DeserializeFromAbstractAsync<ModConfig>(modManagerConfigPath, token);
				if (modManagerConfig != null)
				{
					modData.ApplyModConfig(modManagerConfig);
				}
			}

			//DivinityApp.Log($"Loaded mod '{modData.Name}'.");
			return modData;
		}
		else
		{
			if (metaFile == null)
			{
				DivinityApp.Log($"No meta.lsx for mod pak '{pakPath}'.");
			}
			else
			{
				DivinityApp.Log($"Error: Failed to parse meta.lsx for mod pak '{pakPath}'.");
			}
		}

		return null;
	}

	public static async Task<DivinityModData> LoadModDataFromPakAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods, CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				var pr = new PackageReader();
				using var pak = pr.Read(pakPath);
				return await InternalLoadModDataFromPakAsync(pak, pakPath, builtinMods, token);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak '{pakPath}':\n{ex}");
		}
		return null;
	}

	public static async Task<DivinityModData> LoadModDataFromPakAsync(FileStream stream, string pakPath, Dictionary<string, DivinityModData> builtinMods, CancellationToken token)
	{
		try
		{
			while (!token.IsCancellationRequested)
			{
				stream.Position = 0;
				var pr = new PackageReader();
				using var pak = pr.Read(pakPath, stream);
				return await InternalLoadModDataFromPakAsync(pak, pakPath, builtinMods, token);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak from stream:\n{ex}");
		}
		return null;
	}

	public static async Task<ModLoadingResults> LoadModPackageDataAsync(string modsFolderPath, CancellationToken token)
	{
		var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);

		var results = new ModLoadingResults()
		{
			DirectoryPath = modsFolderPath
		};

		var modPaks = new List<string>();
		try
		{
			//RecursionFilter = (f) => !_IgnoredRecursiveFolders.Any(x => f.FullPath.Contains(x))
			var allPaks = FileUtils.EnumerateFiles(modsFolderPath, FileUtils.FlatSearchOptions, f => f.EndsWith(".pak", SCOMP) && !_IgnoredRecursiveFolders.Any(x => f.Contains(x)));
			_allPaksNames.UnionWith(allPaks.Select(p => Path.GetFileNameWithoutExtension(p)));
			modPaks.AddRange(allPaks.Where(PakIsNotPartial));
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error enumerating pak folder '{modsFolderPath}': {ex}");
		}

		DivinityApp.Log($"Mod Packages: {modPaks.Count}");

		var loadedMods = new ConcurrentBag<DivinityModData>();

		async Task AwaitPartition(IEnumerator<string> partition)
		{
			using (partition)
			{
				while (partition.MoveNext())
				{
					if (token.IsCancellationRequested) return;
					await Task.Yield(); // prevents a sync/hot thread hangup
					var modData = await LoadModDataFromPakAsync(partition.Current, builtinMods, token);
					if (modData != null)
					{
						loadedMods.Add(modData);
					}
				}
			}
		}

		var partitionAmount = Environment.ProcessorCount;
		var currentTime = DateTime.Now;
		DivinityApp.Log($"Split mod loading into {partitionAmount} partitions.");
		await Task.WhenAll(Partitioner.Create(modPaks).GetPartitions(partitionAmount).AsParallel().Select(p => AwaitPartition(p)));

		DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} second(s) to load mod paks.");
		var mods = loadedMods.ToList();
		var dupes = mods.GroupBy(x => x.UUID).Where(g => g.Count() > 1).SelectMany(x => x);
		var lastestDuplicates = new Dictionary<string, DivinityModData>();
		foreach (var m in dupes)
		{
			if (!lastestDuplicates.ContainsKey(m.UUID))
			{
				lastestDuplicates[m.UUID] = m;
			}
			else
			{
				var existing = lastestDuplicates[m.UUID];
				if (m.Version.VersionInt > existing.Version.VersionInt)
				{
					lastestDuplicates[m.UUID] = m;
				}
			}
		}
		results.Duplicates.AddRange(dupes.Where(x => !lastestDuplicates.Values.Contains(x)));
		results.Mods.AddRange(mods.Where(x => !dupes.Contains(x)).Concat(lastestDuplicates.Values).OrderBy(x => x.Name));
		return results;
	}

	private static string GetNodeAttribute(Node node, string key, string defaultValue)
	{
		if (node.Attributes.TryGetValue(key, out var att))
		{
			if (att.Type == AttributeType.TranslatedString)
			{
				TranslatedString ts = (TranslatedString)att.Value;
				return ts.Value;
			}
			return att.Value.ToString();
		}
		return defaultValue;
	}

	private static DivinityGameMasterCampaign ParseGameMasterCampaignMetaFile(Resource meta)
	{
		try
		{
			var headerMajor = HEADER_MAJOR;
			var headerMinor = HEADER_MINOR;
			var headerRevision = HEADER_REVISION;
			var headerBuild = HEADER_BUILD;

			ulong.TryParse(meta.Metadata.MajorVersion.ToString(), out headerMajor);
			ulong.TryParse(meta.Metadata.MinorVersion.ToString(), out headerMinor);
			ulong.TryParse(meta.Metadata.Revision.ToString(), out headerRevision);
			ulong.TryParse(meta.Metadata.BuildNumber.ToString(), out headerBuild);

			if (meta.TryFindNode("ModuleInfo", out var moduleInfoNode))
			{
				var uuid = moduleInfoNode.Attributes.First(x => x.Key == "UUID").Value.Value.ToString();
				var name = moduleInfoNode.Attributes.First(x => x.Key == "Name").Value.Value.ToString();
				var description = moduleInfoNode.Attributes.First(x => x.Key == "Description").Value.Value.ToString();
				var author = moduleInfoNode.Attributes.First(x => x.Key == "Author").Value.Value.ToString();
				/*
				if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
				{
					name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
					description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
					author = "Larian Studios";
				}
				*/

				DivinityGameMasterCampaign data = new()
				{
					UUID = GetNodeAttribute(moduleInfoNode, "UUID", ""),
					Name = GetNodeAttribute(moduleInfoNode, "Name", ""),
					Author = GetNodeAttribute(moduleInfoNode, "Author", ""),
					Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetNodeAttribute(moduleInfoNode, "Version64", ""))),
					Folder = GetNodeAttribute(moduleInfoNode, "Folder", ""),
					Description = GetNodeAttribute(moduleInfoNode, "Description", ""),
					MD5 = GetNodeAttribute(moduleInfoNode, "MD5", ""),
					HeaderVersion = new LarianVersion(headerMajor, headerMinor, headerRevision, headerBuild)
				};
				var tagsText = GetNodeAttribute(moduleInfoNode, "Tags", "");
				if (!String.IsNullOrWhiteSpace(tagsText))
				{
					var tags = tagsText.Split(';');
					data.AddTags(tags);
				}

				if (meta.TryFindNode("Dependencies", out var dependenciesNode))
				{
					foreach (var container in dependenciesNode.Children)
					{
						foreach (var node in container.Value)
						{
							DivinityModDependencyData dependencyMod = new()
							{
								UUID = GetNodeAttribute(node, "UUID", ""),
								Name = GetNodeAttribute(node, "Name", ""),
								Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetNodeAttribute(node, "Version64", ""))),
								Folder = GetNodeAttribute(node, "Folder", ""),
								MD5 = GetNodeAttribute(node, "MD5", "")
							};
							//DivinityApp.LogMessage($"Added dependency to {modData.Name} - {dependencyMod.ToString()}");
							if (dependencyMod.UUID != "")
							{
								data.Dependencies.Add(dependencyMod);
							}
						}
					}
				}

				if (moduleInfoNode.Children.TryGetValue("PublishVersion", out var publishVersionList))
				{
					var publishVersion = LarianVersion.FromInt(SafeConvertStringUnsigned(GetNodeAttribute(publishVersionList.First(), "Version64", "")));
					data.PublishVersion = publishVersion;
				}

				//if (moduleInfoNode.Children.TryGetValue("TargetModes", out var targetModesList))
				//{
				//	foreach (var node in targetModesList)
				//	{
				//		var target = GetNodeAttribute(node, "Object", "");
				//		if (!String.IsNullOrEmpty(target))
				//		{
				//			data.Modes.Add(target);
				//		}
				//	}
				//	data.Targets = string.Join(", ", data.Modes);
				//}

				data.MetaResource = meta;

				return data;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error parsing meta.lsx: {ex}");
		}
		return null;
	}

	private static bool IsMetaFile(string f) => Path.GetFileName(f).Equals("meta.lsf", SCOMP);

	public static List<DivinityGameMasterCampaign> LoadGameMasterData(string folderPath, CancellationToken? token = null)
	{
		List<DivinityGameMasterCampaign> campaignEntries = new();

		if (Directory.Exists(folderPath))
		{
			List<string> campaignMetaFiles = new();
			try
			{
				var files = FileUtils.EnumerateFiles(folderPath, FileUtils.RecursiveOptions, IsMetaFile);
				if (files != null)
				{
					campaignMetaFiles.AddRange(files);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error enumerating GM campaigns folder '{folderPath}':\n{ex}");
			}

			DivinityApp.Log("Campaign meta files: " + campaignMetaFiles.Count());
			foreach (var metaPath in campaignMetaFiles)
			{
				try
				{
					if (token.HasValue && token.Value.IsCancellationRequested)
					{
						return campaignEntries;
					}
					DivinityGameMasterCampaign campaignData = null;

					using var fileStream = new System.IO.FileStream(metaPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
					using var reader = new LSFReader(fileStream);

					campaignData = ParseGameMasterCampaignMetaFile(reader.Read());

					if (campaignData != null)
					{
						campaignData.FilePath = metaPath;
						try
						{
							campaignData.LastModified = File.GetLastWriteTime(metaPath);
						}
						catch (PlatformNotSupportedException ex)
						{
							DivinityApp.Log($"Error getting pak last modified date for '{metaPath}':\n{ex}");
						}
						campaignEntries.Add(campaignData);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading meta file '{metaPath}':\n{ex}");
				}
			}
		}
		return campaignEntries;
	}

	private static Node FindResourceNode(Node node, string attribute, string matchVal)
	{
		if (node.Attributes.TryGetValue(attribute, out var att))
		{
			var attVal = (string)att.Value;
			if (attVal.Equals(matchVal, SCOMP))
			{
				return node;
			}
		}
		foreach (var nList in node.Children.Values)
		{
			foreach (var n in nList)
			{
				var match = FindResourceNode(n, attribute, matchVal);
				if (match != null) return match;
			}
		}
		return null;
	}

	public static async Task<ModSettingsParseResults> LoadModSettingsFileAsync(string path)
	{
		var modOrderUUIDs = new List<string>();
		var activeMods = new List<DivinityProfileActiveModData>();

		if (File.Exists(path))
		{
			Resource modSettingsRes = null;
			try
			{
				modSettingsRes = await LoadResourceAsync(path, ResourceFormat.LSX);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading '{path}':\n{ex}");
			}

			if (modSettingsRes != null && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
			{
				if (region.Children.TryGetValue("ModOrder", out var modOrderRootNode))
				{
					var modOrderChildrenRoot = modOrderRootNode.FirstOrDefault();
					if (modOrderChildrenRoot != null)
					{
						var modOrder = modOrderChildrenRoot.Children.Values.FirstOrDefault();
						if (modOrder != null)
						{
							foreach (var c in modOrder)
							{
								if (c.Attributes.TryGetValue("UUID", out var attribute))
								{
									var uuid = (string)attribute.Value;
									if (!String.IsNullOrEmpty(uuid))
									{
										modOrderUUIDs.Add(uuid);
									}
								}
							}
						}
					}
				}

				if (region.Children.TryGetValue("Mods", out var modListRootNode))
				{
					var modListChildrenRoot = modListRootNode.FirstOrDefault();
					if (modListChildrenRoot != null)
					{
						var modList = modListChildrenRoot.Children.Values.FirstOrDefault();
						if (modList != null)
						{
							foreach (var c in modList)
							{
								var activeModData = new DivinityProfileActiveModData();
								activeModData.LoadFromAttributes(c.Attributes);
								if (!DivinityModDataLoader.IgnoreMod(activeModData.UUID))
								{
									activeMods.Add(activeModData);
								}
							}
						}
					}
				}
			}
		}

		return new ModSettingsParseResults()
		{
			ModOrder = modOrderUUIDs,
			ActiveMods = activeMods,
		};
	}

	public static async Task<List<DivinityProfileData>> LoadProfileDataAsync(string profilePath)
	{
		List<DivinityProfileData> profiles = [];
		if (Directory.Exists(profilePath))
		{
			var profileDirectories = Directory.EnumerateDirectories(profilePath);
			foreach (var folder in profileDirectories)
			{
				var folderName = Path.GetFileName(folder);
				var name = folderName;
				var displayedName = folderName;
				var profileUUID = "";

				var profileFile = GetProfileFile(folder);
				if (profileFile != null)
				{
					var profileRes = await LoadResourceAsync(profileFile.FullName);
					if (profileRes != null && profileRes.Regions.TryGetValue("PlayerProfile", out var region))
					{
						if (region.Attributes.TryGetValue("PlayerProfileName", out var profileNameAtt))
						{
							name = (string)profileNameAtt.Value;
						}
						if (region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
						{
							displayedName = (string)profileDisplayNameAtt.Value;
						}
						if (region.Attributes.TryGetValue("PlayerProfileID", out var profileIdAtt))
						{
							profileUUID = (string)profileIdAtt.Value;
						}
					}
				}

				if (String.IsNullOrEmpty(name))
				{
					name = folderName;
				}

				if (String.IsNullOrEmpty(displayedName))
				{
					displayedName = name;
				}

				var profileData = new DivinityProfileData()
				{
					Name = name,
					FolderName = folderName,
					ProfileName = displayedName,
					UUID = profileUUID,
					FilePath = Path.GetFullPath(folder)
				};

				var modSettingsFile = Path.Join(folder, "modsettings.lsx");
				var modSettings = await LoadModSettingsFileAsync(modSettingsFile);
				profileData.ModOrder.AddRange(modSettings.ModOrder);
				profileData.ActiveMods.AddRange(modSettings.ActiveMods);

				profiles.Add(profileData);
			}
		}
		return profiles;
	}

	public static async Task<Resource> LoadResourceAsync(string path)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = ResourceUtils.LoadResource(path, _loadParams);
				return resource;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{path}': {ex}");
				return null;
			}
		});
	}

	public static async Task<Resource> LoadResourceAsync(string path, ResourceFormat resourceFormat)
	{
		try
		{
			using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			await fs.ReadAsync(new byte[fs.Length], 0, (int)fs.Length);
			fs.Position = 0;
			var resource = ResourceUtils.LoadResource(fs, resourceFormat, _loadParams);
			return resource;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading '{path}': {ex}");
			return null;
		}
	}

	public static async Task<Resource> LoadResourceAsync(System.IO.Stream stream, ResourceFormat resourceFormat)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = ResourceUtils.LoadResource(stream, resourceFormat, _loadParams);
				return resource;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading resource: {ex}");
				return null;
			}
		});
	}

	private static FileInfo GetProfileFile(string path)
	{
		var files = FileUtils.EnumerateFiles(path, null, (f) =>
		{
			var name = Path.GetFileName(f);
			if (name.IndexOf("profile", SCOMP) > -1 && LarianFileTypes.Any(e => name.EndsWith(e, SCOMP)))
			{
				return true;
			}
			return false;
		}).Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
		return files.FirstOrDefault();
	}

	private static FileInfo GetPlayerProfilesFile(string path)
	{
		var files = FileUtils.EnumerateFiles(path, null, (f) =>
		{
			var name = Path.GetFileName(f);
			if (name.IndexOf("playerprofiles", SCOMP) > -1 && LarianFileTypes.Any(e => name.EndsWith(e, SCOMP)))
			{
				return true;
			}
			return false;
		}).Select(x => new FileInfo(x)).OrderBy(x => x.LastWriteTime).ToList();
		return files.FirstOrDefault();
	}

	public static bool ExportedSelectedProfile(string profilePath, string profileUUID)
	{
		var conversionParams = ResourceConversionParameters.FromGameVersion(DivinityApp.GAME);
		var playerprofilesFile = GetPlayerProfilesFile(profilePath);
		if (playerprofilesFile != null)
		{
			try
			{
				var res = ResourceUtils.LoadResource(playerprofilesFile.FullName, _loadParams);
				if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
				{
					if (region.Attributes.TryGetValue("ActiveProfile", out var att))
					{
						att.Value = profileUUID;
						ResourceUtils.SaveResource(res, playerprofilesFile.FullName, conversionParams);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error saving {playerprofilesFile}: {ex}");
			}
		}
		else
		{
			DivinityApp.Log($"[*WARNING*] '{playerprofilesFile}' does not exist. Skipping selected profile saving.");
		}
		return false;
	}

	public static async Task<string> GetSelectedProfileUUIDAsync(string profilePath)
	{
		var playerprofilesFile = GetPlayerProfilesFile(profilePath);
		string activeProfileUUID = "";
		if (playerprofilesFile != null)
		{
			DivinityApp.Log($"Loading playerprofiles at '{playerprofilesFile}'");
			var res = await LoadResourceAsync(playerprofilesFile.FullName);
			if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
			{
				//DivinityApp.LogMessage($"ActiveProfile | Getting root node '{String.Join(";", region.Attributes.Keys)}'");

				if (region.Attributes.TryGetValue("ActiveProfile", out var att))
				{
					DivinityApp.Log($"ActiveProfile | '{att.Value}'");
					activeProfileUUID = (string)att.Value;
				}
			}
		}
		else
		{
			DivinityApp.Log("No playerprofilesFile found.");
		}
		return activeProfileUUID;
	}

	public static bool ExportLoadOrderToFile(string outputFilePath, DivinityLoadOrder order)
	{
		var parentDir = Path.GetDirectoryName(outputFilePath);
		if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

		string contents = JsonConvert.SerializeObject(order, Newtonsoft.Json.Formatting.Indented);

		FileUtils.WriteTextFile(outputFilePath, contents);

		order.FilePath = outputFilePath;

		return true;
	}

	public static async Task<bool> ExportLoadOrderToFileAsync(string outputFilePath, DivinityLoadOrder order)
	{
		var parentDir = Path.GetDirectoryName(outputFilePath);
		if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

		string contents = JsonConvert.SerializeObject(order, Newtonsoft.Json.Formatting.Indented);

		await FileUtils.WriteTextFileAsync(outputFilePath, contents);

		order.FilePath = outputFilePath;

		return true;
	}

	public static List<DivinityLoadOrder> FindLoadOrderFilesInDirectory(string directory)
	{
		List<DivinityLoadOrder> loadOrders = new();

		if (Directory.Exists(directory))
		{
			var files = FileUtils.EnumerateFiles(directory, FileUtils.RecursiveOptions, f => f.EndsWith(".json", SCOMP));

			foreach (var loadOrderFile in files)
			{
				try
				{
					var fileText = File.ReadAllText(loadOrderFile);
					DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
					if (order != null)
					{
						order.FilePath = loadOrderFile;
						order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);
						loadOrders.Add(order);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Failed to read '{loadOrderFile}': {ex}");
				}
			}
		}

		return loadOrders;
	}

	public static async Task<List<DivinityLoadOrder>> FindLoadOrderFilesInDirectoryAsync(string directory)
	{
		List<DivinityLoadOrder> loadOrders = new();

		if (Directory.Exists(directory))
		{
			var options = new EnumerationOptions()
			{
				RecurseSubdirectories = true
			};
			var files = FileUtils.EnumerateFiles(directory, options,
				(f) => f.EndsWith(".json", SCOMP) && !f.Equals("settings.json", SCOMP));

			foreach (var loadOrderFile in files)
			{
				try
				{
					using var reader = File.OpenText(loadOrderFile);
					var fileText = await reader.ReadToEndAsync();

					DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
					order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
					if (order != null)
					{
						order.FilePath = loadOrderFile;
						order.LastModifiedDate = File.GetLastWriteTime(loadOrderFile);

						loadOrders.Add(order);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Failed to read '{loadOrderFile}': {ex}");
				}
			}
		}

		return loadOrders;
	}
	public static async Task<DivinityLoadOrder> LoadOrderFromFileAsync(string loadOrderFile)
	{
		if (File.Exists(loadOrderFile))
		{
			try
			{
				using var reader = File.OpenText(loadOrderFile);
				var fileText = await reader.ReadToEndAsync();
				DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
				if (order != null)
				{
					order.FilePath = loadOrderFile;
				}
				return order;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{loadOrderFile}': {ex}");
			}
		}
		return null;
	}

	public static DivinityLoadOrder LoadOrderFromFile(string loadOrderFile, IEnumerable<DivinityModData> allMods)
	{
		var ext = Path.GetExtension(loadOrderFile).ToLower();
		DivinityLoadOrder order = null;
		switch (ext)
		{
			case ".json":
				if (DivinityJsonUtils.TrySafeDeserializeFromPath<DivinityLoadOrder>(loadOrderFile, out var savedOrder))
				{
					return savedOrder;
				}
				else
				{
					if (DivinityJsonUtils.TrySafeDeserializeFromPath<List<DivinitySerializedModData>>(loadOrderFile, out var exportedOrder))
					{
						order = new DivinityLoadOrder
						{
							IsDecipheredOrder = true
						};
						order.AddRange(exportedOrder);
						DivinityApp.Log(String.Join("\n", order.Order.Select(x => x.UUID)));
						var modGUIDs = allMods.Select(x => x.UUID).ToHashSet();
						foreach (var entry in order.Order)
						{
							if (!modGUIDs.Contains(entry.UUID))
							{
								entry.Missing = true;
							}
						}
						order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
						return order;
					}
				}
				break;
			case ".txt":
				var textPattern = new Regex(@"\((\S+\.pak)\)", RegexOptions.IgnoreCase);
				var textLines = File.ReadAllLines(loadOrderFile);
				order = new DivinityLoadOrder();
				foreach (var line in textLines)
				{
					var match = textPattern.Match(line);
					if (match.Success)
					{
						var isOverride = line.Substring(0, 8) == "Override";
						var pakName = Path.GetFileName(match.Groups[1].Value.Trim());
						var mod = allMods.FirstOrDefault(x => x.PakEquals(pakName, SCOMP));
						DivinityApp.Log($"isOverride({isOverride}) Sub test: [{line.Substring(0, 8)}] pakName({pakName}) mod({mod})");
						if (mod != null)
						{
							if (!isOverride)
							{
								order.Add(mod);
							}
						}
						else
						{
							order.Order.Add(new DivinityLoadOrderEntry
							{
								Missing = true,
								Name = pakName,
								UUID = "",
							});
						}
					}
				}
				break;
			case ".tsv":
				var tsvLines = File.ReadAllLines(loadOrderFile);
				var header = tsvLines[0].Split('\t');
				var fileIndex = header.IndexOf("FileName");
				var nameIndex = header.IndexOf("Name");
				var urlIndex = header.IndexOf("URL");
				if (fileIndex > -1)
				{
					order = new DivinityLoadOrder();
					for (var i = 1; i < tsvLines.Length; i++)
					{
						var line = tsvLines[i];
						var lineData = line.Split('\t');
						if (lineData.Length > fileIndex)
						{
							var isOverride = line.Substring(0, 8) == "Override";
							var fileName = Path.GetFileName(lineData[fileIndex].Trim());
							var mod = allMods.FirstOrDefault(x => x.PakEquals(fileName, SCOMP));
							if (mod != null)
							{
								if (!isOverride)
								{
									order.Add(mod);
								}
							}
							else
							{
								var name = fileName;
								if (nameIndex > -1)
								{
									name = lineData[nameIndex];
								}
								if (urlIndex > -1 && lineData.Length > urlIndex)
								{
									name = $"{name} {lineData[urlIndex]}";
								}
								order.Order.Add(new DivinityLoadOrderEntry
								{
									Missing = true,
									Name = name,
									UUID = "",
								});
							}
						}
					}
				}
				break;
		}
		if (order != null)
		{
			order.IsDecipheredOrder = true;
			order.Name = Path.GetFileNameWithoutExtension(loadOrderFile);
		}
		return order;
	}

	public static async Task<bool> ExportModSettingsToFileAsync(string folder, IEnumerable<IModEntry> order)
	{
		if (Directory.Exists(folder))
		{
			string outputFilePath = Path.Join(folder, "modsettings.lsx");
			string contents = GenerateModSettingsFile(order);
			try
			{
				//Lazy indentation!
				var xml = new XmlDocument();
				xml.LoadXml(contents);
				using var sw = new System.IO.StringWriter();
				using var xw = new XmlTextWriter(sw);
				xw.Formatting = System.Xml.Formatting.Indented;
				xw.Indentation = 2;
				xml.WriteTo(xw);

				await FileUtils.WriteTextFileAsync(outputFilePath, sw.ToString());

				return true;
			}
			catch (AccessViolationException ex)
			{
				DivinityApp.Log($"Failed to write file '{outputFilePath}': {ex}");
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error exporting file '{outputFilePath}': {ex}");
			}
		}
		return false;
	}

	public static async Task<bool> UpdateLauncherPreferencesAsync(string appDataLarianFolder, bool enableTelemetry, bool enableModWarnings, bool force = false)
	{
		Dictionary<string, object> settings = null;
		//Patch 7 changes this to "Larian Studios" instead of "LarianStudios"
		var folderDir = Path.Join(appDataLarianFolder, @"Launcher\Settings");
		var settingsFilePath = Path.Join(folderDir, "preferences.json");
		if (File.Exists(settingsFilePath))
		{
			settings = DivinityJsonUtils.SafeDeserializeFromPath<Dictionary<string, object>>(settingsFilePath);
		}
		if (settings == null)
		{
			DivinityApp.Log($"Failed to load launcher preferences at '{settingsFilePath}'. File may be locked / may not exist.");
			return false;
		}
		settings["SendStats"] = enableTelemetry;
		if (force || !enableModWarnings)
		{
			settings["ModsWarningShown"] = !enableModWarnings;
			settings["DataWarningShown"] = !enableModWarnings;
			settings["DisplayFilesValidationMsg"] = enableModWarnings;
			settings["DisplayModsDetectedMsg"] = enableModWarnings;
		}

		string contents = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);

		await FileUtils.WriteTextFileAsync(settingsFilePath, contents);

		DivinityApp.Log($"Updated {settingsFilePath}");
		return true;
	}

	public static List<DivinityModData> GetDependencyMods(DivinityModData mod, IEnumerable<DivinityModData> allMods, HashSet<string> addedMods, bool forExport = true)
	{
		List<DivinityModData> mods = new();
		var dependencies = mod.Dependencies.Items.Where(x => !IgnoreModDependency(x.UUID));
		foreach (var d in dependencies)
		{
			var dependencyModData = allMods.FirstOrDefault(x => x.UUID == d.UUID);
			if (dependencyModData != null)
			{
				var dependencyMods = GetDependencyMods(dependencyModData, allMods, addedMods);
				if (dependencyMods.Count > 0)
				{
					foreach (var m in dependencyMods)
					{
						if ((!forExport || m.CanAddToLoadOrder) && !addedMods.Contains(m.UUID))
						{
							addedMods.Add(m.UUID);
							mods.Add(m);
						}
					}
				}
				if ((!forExport || dependencyModData.CanAddToLoadOrder) && !addedMods.Contains(dependencyModData.UUID))
				{
					mods.Add(dependencyModData);
					addedMods.Add(dependencyModData.UUID);
				}
			}
		}
		return mods;
	}

	public static List<DivinityModData> BuildOutputList(IEnumerable<DivinityLoadOrderEntry> order, IEnumerable<DivinityModData> allMods, bool addDependencies = true, DivinityModData selectedAdventure = null)
	{
		List<DivinityModData> orderList = new();
		var addedMods = new HashSet<string>();

		if (selectedAdventure != null)
		{
			if (addDependencies && selectedAdventure.HasDependencies)
			{
				orderList.AddRange(GetDependencyMods(selectedAdventure, allMods, addedMods));
			}
			orderList.Add(selectedAdventure);
			addedMods.Add(selectedAdventure.UUID);
		}

		foreach (var m in order.Where(x => !x.Missing))
		{
			var mData = allMods.FirstOrDefault(x => x.UUID == m.UUID);
			if (mData != null)
			{
				if (addDependencies && mData.HasDependencies)
				{
					orderList.AddRange(GetDependencyMods(mData, allMods, addedMods));
				}

				if (!addedMods.Contains(mData.UUID))
				{
					orderList.Add(mData);
					addedMods.Add(mData.UUID);
				}
			}
			else
			{
				DivinityApp.Log($"[*ERROR*] Missing mod for mod in order: '{m.Name}'.");
			}
		}

		return orderList;
	}

	public static string GenerateModSettingsFile(IEnumerable<IModEntry> orderList)
	{
		/* Active mods are contained within the "ModOrder" node.*/
		string modulesText = "";
		/* The "Mods" node is used for the in-game menu it seems. The selected adventure mod is always at the top. */
		string modShortDescText = "";

		foreach (var mod in orderList)
		{
			if (!String.IsNullOrWhiteSpace(mod.UUID))
			{
				//Use Export to support lists with categories/things that don't need exporting
				var modText = mod.Export(ModExportType.XML);
				if(!String.IsNullOrEmpty(modText))
				{
					modulesText += String.Format(DivinityApp.XML_MOD_ORDER_MODULE, mod.UUID) + Environment.NewLine;
					modShortDescText += modText + Environment.NewLine;
				}
			}
		}

		return String.Format(DivinityApp.XML_MOD_SETTINGS_TEMPLATE, modulesText, modShortDescText);
	}

	public static string CreateHandle()
	{
		return Guid.NewGuid().ToString().Replace('-', 'g').Insert(0, "h");
	}

	private static Node FindNode(Node node, string name)
	{
		if (node.Name.Equals(name, SCOMP))
		{
			return node;
		}
		else
		{
			return FindNode(node.Children, name);
		}
	}

	private static Node FindNode(Dictionary<string, List<Node>> children, string name)
	{
		foreach (var kvp in children)
		{
			if (kvp.Key.Equals(name, SCOMP))
			{
				return kvp.Value.FirstOrDefault();
			}

			foreach (var node in kvp.Value)
			{
				var match = FindNode(node, name);
				if (match != null)
				{
					return match;
				}
			}
		}
		return null;
	}

	private static Node FindNode(Region region, string name)
	{
		foreach (var kvp in region.Children)
		{
			if (kvp.Key.Equals(name, SCOMP))
			{
				return kvp.Value.First();
			}
		}

		var match = FindNode(region.Children, name);
		if (match != null)
		{
			return match;
		}

		return null;
	}

	private static Node FindNode(Resource resource, string name)
	{
		foreach (var region in resource.Regions.Values)
		{
			var match = FindNode(region, name);
			if (match != null)
			{
				return match;
			}
		}

		return null;
	}

	public static DivinityLoadOrder GetLoadOrderFromSave(string file, string ordersFolder = "")
	{
		try
		{
			var reader = new PackageReader();
			using var package = reader.Read(file);
			PackagedFileInfo PackagedFileInfo = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
			if (PackagedFileInfo == null)
			{
				return null;
			}

			Resource resource = null;

			System.IO.Stream rsrcStream = PackagedFileInfo.CreateContentReader();
			try
			{
				using var rsrcReader = new LSFReader(rsrcStream);
				resource = rsrcReader.Read();
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading file:\n{ex}");
			}

			if (resource != null)
			{
				var modListChildrenRoot = FindNode(resource, "Mods");

				if (modListChildrenRoot != null)
				{
					var modList = modListChildrenRoot.Children.Values.FirstOrDefault();
					if (modList != null && modList.Count > 0)
					{
						var fileName = Path.GetFileNameWithoutExtension(file);
						string orderName = fileName;
						var re = new Regex(@".*PlayerProfiles\\(.*?)\\Savegames.*");
						var match = re.Match(Path.GetFullPath(file));
						if (match.Success)
						{
							orderName = $"{match.Groups[1].Value}_{fileName}";
						}
						DivinityLoadOrder loadOrder = new()
						{
							Name = orderName,
							FilePath = Path.Join(ordersFolder, MakeSafeFilename(Path.Join(orderName + ".json"), '_'))
						};

						foreach (var c in modList)
						{
							string name = "";
							string uuid = null;
							if (c.Attributes.TryGetValue("UUID", out var idAtt))
							{
								uuid = (string)idAtt.Value;
							}

							if (c.Attributes.TryGetValue("Name", out var nameAtt))
							{
								name = (string)nameAtt.Value;
							}

							if (uuid != null && !IgnoreMod(uuid))
							{
								DivinityApp.Log($"Found mod in save: '{name}_{uuid}'.");
								loadOrder.Order.Add(new DivinityLoadOrderEntry()
								{
									UUID = uuid,
									Name = name
								});
							}
							else
							{
								DivinityApp.Log($"Ignoring mod in save: '{name}_{uuid}'.");
							}
						}

						if (loadOrder.Order.Count > 0)
						{
							return loadOrder;
						}
					}
				}
				else
				{
					DivinityApp.Log($"Couldn't find Mods node '{String.Join(";", resource.Regions.Values.First().Children.Keys)}'.");
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error parsing save '{file}':\n{ex}");
		}

		return null;
	}

	private readonly static List<string> _fallbackFeatureFlags = new();

	private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(string configFile)
	{
		try
		{
			using var reader = File.OpenText(configFile);
			var text = await reader.ReadToEndAsync();
			if (!String.IsNullOrWhiteSpace(text))
			{
				var config = DivinityJsonUtils.SafeDeserialize<DivinityModScriptExtenderConfig>(text);
				if (config != null)
				{
					return config;
				}
				else
				{
					DivinityApp.Log($"Error reading '{configFile}'. Trying to manually read json text.");
					var jsonObj = JObject.Parse(text);
					if (jsonObj != null)
					{
						config = new DivinityModScriptExtenderConfig
						{
							RequiredVersion = jsonObj.GetValue("RequiredExtensionVersion", -1)
						};
						config.FeatureFlags.AddRange(jsonObj.GetValue("FeatureFlags", _fallbackFeatureFlags));
						return config;
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading '{configFile}': {ex}");
		}
		return null;
	}

	private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(Stream stream)
	{
		try
		{
			using var sr = new StreamReader(stream);
			string text = await sr.ReadToEndAsync();
			if (!String.IsNullOrWhiteSpace(text))
			{
				var config = DivinityJsonUtils.SafeDeserialize<DivinityModScriptExtenderConfig>(text);
				if (config != null)
				{
					return config;
				}
				else
				{
					DivinityApp.Log($"Error reading Config.json. Trying to manually read json text.");
					var jsonObj = JObject.Parse(text);
					if (jsonObj != null)
					{
						config = new DivinityModScriptExtenderConfig
						{
							RequiredVersion = jsonObj.GetValue("RequiredExtensionVersion", -1)
						};
						config.FeatureFlags.AddRange(jsonObj.GetValue("FeatureFlags", _fallbackFeatureFlags));
						return config;
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading 'ScriptExtenderConfig.json': {ex}");
		}
		return null;
	}

	private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(PackagedFileInfo configFile)
	{
		try
		{
			using var stream = configFile.CreateContentReader();
			return await LoadScriptExtenderConfigAsync(stream);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading 'ScriptExtenderConfig.json': {ex}");
		}
		return null;
	}

	private static async Task<DivinityModData> LoadModFromModInfo(VFS vfs, ModInfo modInfo, CancellationToken token)
	{
		if (vfs.TryOpen(modInfo.Meta, out var stream))
		{
			using var sr = new System.IO.StreamReader(stream);
			if (token.IsCancellationRequested) return null;
			string text = await sr.ReadToEndAsync();
			var modData = ParseMetaFile(text);
			if (modData != null)
			{
				var filePath = modInfo.PackagePath;
				if (String.IsNullOrEmpty(filePath))
				{
					filePath = modInfo.ModsPath;
				}
				else
				{
					try
					{
						modData.LastModified = File.GetLastWriteTime(filePath);
						modData.LastUpdated = modData.LastModified;
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error getting last modified date for '{filePath}': {ex}");
					}
				}

				if (!String.IsNullOrEmpty(filePath))
				{
					modData.FilePath = filePath;
					var fileTimeFile = filePath;

					if (Path.Equals(filePath, modInfo.ModsPath))
					{
						modData.IsEditorMod = true;
						fileTimeFile = modInfo.Meta;
					}

					try
					{
						modData.LastModified = File.GetLastWriteTime(fileTimeFile);
						modData.LastUpdated = modData.LastModified;
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error getting last modified date for '{modData.FilePath}': {ex}");
					}
				}
				modData.IsLarianMod = modData.Author.Contains("Larian") || String.IsNullOrEmpty(modData.Author);
				var isBaseMod = DivinityApp.IgnoredMods.Any(x => x.UUID == modData.UUID) || modData.IsLarianMod;
				if (isBaseMod)
				{
					modData.SetIsBaseGameMod(true);
					DivinityApp.Log($"Added base mod: Name({modData.Name}) UUID({modData.UUID}) Author({modData.Author}) Version({modData.Version.VersionInt})");
					DivinityApp.Log($"CanAddToLoadOrder({modData.CanAddToLoadOrder}) IsHidden({modData.IsHidden})");
				}
				else
				{
					modData.IsUserMod = true;
					DivinityApp.Log($"Added mod from data folder: Name({modData.Name}) UUID({modData.UUID}) Author({modData.Author}) Version({modData.Version.VersionInt})");
				}

				if (vfs.DirectoryExists(modInfo.ModsPath))
				{
					await TryLoadConfigFiles(vfs, modData, modInfo.ModsPath, token);
				}

				return modData;
			}
		}
		return null;
	}

	public static async Task<List<DivinityModData>> LoadBuiltinModsAsync(string gameDataPath, CancellationToken token)
	{
		List<DivinityModData> baseMods = [];

		try
		{
			var vfs = new VFS();
			vfs.AttachGameDirectory(gameDataPath, true);
			vfs.FinishBuild();

			var modResources = new ModResources();
			var modHelper = new ModPathVisitor(modResources, vfs)
			{
				Game = DivinityApp.GAME_COMPILER,
				CollectGlobals = false,
				CollectLevels = false,
				CollectStoryGoals = false,
				CollectStats = false
			};

			modHelper.Discover();

			if (modResources.Mods != null && modResources.Mods.Values != null)
			{
				var currentTime = DateTime.Now;
				var count = modResources.Mods.Values.Count;
				foreach (var modInfo in modResources.Mods.Values)
				{
					var modData = await LoadModFromModInfo(vfs, modInfo, token);
					if (modData != null)
					{
						if(modInfo.PackagePath?.EndsWith(".pak") == true)
						{

						}
						baseMods.Add(modData);
					}
				}
				DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} seconds(s) to load ({count}) builtin mods.");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error parsing base game mods:\n" + ex.ToString());
		}

		return baseMods;
	}

	public static ModuleInfo TryGetMetaFromPakFileStream(FileStream stream, string filePath, CancellationToken token)
	{
		stream.Position = 0;
		var pr = new PackageReader();
		using var pak = pr.Read(filePath, stream);
		if (pak != null && pak.Files != null)
		{
			for (int i = 0; i < pak.Files.Count; i++)
			{
				if (token.IsCancellationRequested) return null;
				var f = pak.Files[i];

				if (IsModMetaFile(f))
				{
					using var metaStream = f.CreateContentReader();
					using var lsxReader = new LSXReader(metaStream);
					_loadParams.ToSerializationSettings(lsxReader.SerializationSettings);
					var resource = lsxReader.Read();
					if (resource != null)
					{
						return ModuleInfo.FromResource(resource);
					}
				}
			}
		}
		return null;
	}
}
