using DynamicData;

using LSLib.LS;
using LSLib.LS.Enums;

using ModManager.Extensions;
using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.Mod.Game;
using ModManager.Util.Pak;

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ModManager.Util;

public static partial class ModDataLoader
{
	private static readonly StringComparison SCOMP = StringComparison.OrdinalIgnoreCase;
	private static readonly string[] LarianFileTypes = [".lsb", ".lsf", ".lsx", ".lsj"];

	private static readonly ulong HEADER_MAJOR = 4;
	private static readonly ulong HEADER_MINOR = 7;
	private static readonly ulong HEADER_REVISION = 1;
	private static readonly ulong HEADER_BUILD = 3;

	private static readonly string[] VersionAttributes = ["Version64", "Version"];

	public static readonly HashSet<string> IgnoreBuiltinPath = [];
	private static readonly HashSet<string> _allPaksNames = [];

	private static readonly ResourceLoadParameters _loadParams = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
	private static readonly ResourceLoadParameters _modSettingsParams = new() { ByteSwapGuids = false };
	private static readonly NodeSerializationSettings _lsfSerializationSettings = new() { ByteSwapGuids = true, DefaultByteSwapGuids = true };

	[GeneratedRegex("^(Mods|Public|Generated)/(.+?)/.+$")]
	private static partial Regex ModFolderPattern();

	[GeneratedRegex("(_[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-US")]
	private static partial Regex MultiPartPakPattern();

	[GeneratedRegex("^Mods/([^/]+)/meta.lsx", RegexOptions.IgnoreCase)]
	private static partial Regex ModMetaPattern();

	[GeneratedRegex("value=\"(.*?)\"")]
	private static partial Regex XamlValuePattern();

	[GeneratedRegex(@".*PlayerProfiles\\(.*?)\\Savegames.*")]
	private static partial Regex PlayerProfilePathPattern();

	private static readonly Regex _multiPartPakPatternNoExtension = MultiPartPakPattern();
	private static readonly Regex _modMetaPattern = ModMetaPattern();
	private static readonly Regex _ModFolderPattern = ModFolderPattern();

	private static readonly JsonSerializerOptions _indentedJsonSettings = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

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
		return DivinityApp.IgnoredMods.Any(m => m.Folder?.Equals(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), SCOMP) == true);
	}

	public static string MakeSafeFilename(string filename, char replaceChar)
	{
		foreach (var c in Path.GetInvalidFileNameChars())
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
		var att = node.Elements("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
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
			var att = node.Elements("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
			if (att != null)
			{
				return att;
			}
		}
		return fallbackValue;
	}

	private static ulong GetAttributeValueWithId(XElement node, string id, ulong fallbackValue = 0ul)
	{
		var attValue = node.Elements("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
		if (attValue != null && ulong.TryParse(attValue, out var value))
		{
			return value;
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
		if (!String.IsNullOrWhiteSpace(str) && UInt64.TryParse(str, out var val))
		{
			return val;
		}
		return 0UL;
	}

	public static string EscapeXml(string s)
	{
		var toxml = s;
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

	private static ModData? ParseMetaFile(string metaContents, string? filePath = null)
	{
		try
		{
			var xDoc = XElement.Parse(EscapeXmlAttributes(metaContents));
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

				ModData modData = new()
				{
					HasMetadata = true,
					UUID = uuid,
					Name = name,
					Author = author,
					Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(moduleInfoNode, VersionAttributes, ""))),
					Folder = GetAttributeWithId(moduleInfoNode, "Folder", ""),
					Description = description,
					MD5 = GetAttributeWithId(moduleInfoNode, "MD5", ""),
					PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
					FileSize = GetAttributeValueWithId(moduleInfoNode, "FileSize", 0ul),
					ModType = "Add-on",
					HeaderVersion = new LarianVersion(headerMajor, headerMinor, headerRevision, headerBuild)
				};

				//Patch 7 removed the "Type" attribute
				//if(uuid == DivinityApp.MAIN_CAMPAIGN_UUID)
				//{
				//	modData.ModType = "Adventure";
				//}

				var tagsText = GetAttributeWithId(moduleInfoNode, "Tags", "");
				if (!String.IsNullOrWhiteSpace(tagsText))
				{
					modData.AddTags(tagsText.Split(';'));
				}

				var dependenciesRoot = xDoc.Descendants("node").FirstOrDefault(x => x.Attribute("id")?.Value == "Dependencies");

				if(dependenciesRoot != null)
				{
					var dependenciesNodes = dependenciesRoot.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (dependenciesNodes != null)
					{
						foreach (var node in dependenciesNodes)
						{
							ModuleShortDesc entryMod = new()
							{
								Folder = GetAttributeWithId(node, "Folder", ""),
								MD5 = GetAttributeWithId(node, "MD5", ""),
								Name = UnescapeXml(GetAttributeWithId(node, "Name", "")),
								PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
								UUID = GetAttributeWithId(node, "UUID", ""),
								Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(node, VersionAttributes, ""))),
							};

							if (!String.IsNullOrEmpty(entryMod.UUID))
							{
								modData.Dependencies.Add(entryMod);
							}
						}
					}
				}

				var conflictsRoot = xDoc.Descendants("node").FirstOrDefault(x => x.Attribute("id")?.Value == "Conflicts");

				if (conflictsRoot != null)
				{
					var conflictsNodes = conflictsRoot.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (conflictsNodes != null)
					{
						foreach (var node in conflictsNodes)
						{
							ModuleShortDesc entryMod = new()
							{
								Folder = GetAttributeWithId(node, "Folder", ""),
								MD5 = GetAttributeWithId(node, "MD5", ""),
								Name = UnescapeXml(GetAttributeWithId(node, "Name", "")),
								PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
								UUID = GetAttributeWithId(node, "UUID", ""),
								Version = LarianVersion.FromInt(SafeConvertStringUnsigned(GetAttributeWithId(node, VersionAttributes, ""))),
							};

							if (!String.IsNullOrEmpty(entryMod.UUID))
							{
								modData.Conflicts.Add(entryMod);
							}
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

				return modData;
			}
			else
			{
				DivinityApp.Log($"**[ERROR] ModuleInfo node not found for meta.lsx in mod ({filePath}):\n{metaContents}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error parsing meta.lsx: {ex}");
		}
		return null;
	}

	private static async Task TryLoadConfigFiles(VFS vfs, ModData modData, string modsFolder, CancellationToken token)
	{
		Stream? extenderConfigStream = null;
		Stream? modManagerConfigStream = null;

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
				var modManagerConfig = await JsonUtils.DeserializeFromAbstractAsync<ModConfig>(modManagerConfigStream, token);
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

	public static async Task<ModData> GetModDataFromMeta(PackagedFileInfo file)
	{
		using var stream = file.CreateContentReader();
		using var sr = new StreamReader(stream);
		var text = await sr.ReadToEndAsync();
		return ParseMetaFile(text, file.Name);
	}

	public static async Task<ModData?> GetModDataFromMeta(string filePath, CancellationToken token)
	{
		var fileBytes = await FileUtils.LoadFileAsBytesAsync(filePath, token);
		if (fileBytes != null)
		{
			var contents = Encoding.UTF8.GetString(fileBytes);
			if (contents.IsValid())
			{
				return ParseMetaFile(contents, filePath);
			}
		}
		return null;
	}

	private static async Task<List<ModData>> InternalLoadModDataFromPakAsync(Package pak, string pakPath,
		Dictionary<string, ModData>? builtinMods, CancellationToken token, bool skipFileParsing = false)
	{
		var pakName = Path.GetFileNameWithoutExtension(pakPath);

		builtinMods ??= [];

		var metaFiles = new List<PackagedFileInfo>();
		var hasBuiltinDirectory = false;
		var isOverridingBuiltinDirectory = false;
		var hasModFolderData = false;
		var hasOsirisScripts = DivinityOsirisModStatus.NONE;
		var builtinModOverrides = new Dictionary<string, ModData>();
		var files = new HashSet<string>();
		var baseGameFiles = new HashSet<string>();

		PackagedFileInfo? extenderConfigPath = null;
		PackagedFileInfo? modManagerConfigPath = null;

		var fileModified = DateTimeOffset.Now;
		if (File.Exists(pakPath))
		{
			try
			{
				fileModified = File.GetLastWriteTime(pakPath);
			}
			catch (PlatformNotSupportedException ex)
			{
				DivinityApp.Log($"Error getting pak last modified date for '{pakPath}': {ex}");
			}
		}

		if (pak != null && pak.Files != null)
		{
			if (skipFileParsing)
			{
				metaFiles = pak.Files.Where(x => x.Name.EndsWith("meta.lsx")).ToList();
			}
			else
			{
				for (var i = 0; i < pak.Files.Count; i++)
				{
					if (token.IsCancellationRequested) break;
					var f = pak.Files[i];
					files.Add(f.Name);

					//var entryFile = f.Name.Replace(@"\", "/");
					var entryFile = f.Name;

					if (entryFile.EndsWith(DivinityApp.EXTENDER_MOD_CONFIG, StringComparison.OrdinalIgnoreCase))
					{
						extenderConfigPath = f;
					}
					else if (entryFile.EndsWith(ModConfig.FileName, StringComparison.OrdinalIgnoreCase) && Path.GetDirectoryName(entryFile) == "Mods")
					{
						modManagerConfigPath = f;
					}
					else if (IsModMetaFile(f))
					{
						metaFiles.Add(f);
					}
					else
					{
						var modFolderMatch = _ModFolderPattern.Match(entryFile);
						if (modFolderMatch.Success)
						{
							var modFolder = Path.GetFileName(modFolderMatch.Groups[2].Value.TrimEnd(Path.DirectorySeparatorChar));
							if (entryFile.EndsWith($"Mods/{modFolder}/{ModConfig.FileName}", StringComparison.OrdinalIgnoreCase))
							{
								modManagerConfigPath = f;
							}
							else if (entryFile.Contains($"Mods/{modFolder}/Story/RawFiles/Goals", StringComparison.OrdinalIgnoreCase))
							{
								if (hasOsirisScripts == DivinityOsirisModStatus.NONE)
								{
									hasOsirisScripts = DivinityOsirisModStatus.SCRIPTS;
								}
								if (entryFile.EndsWith("ForceRecompile.txt", StringComparison.OrdinalIgnoreCase))
								{
									hasOsirisScripts = DivinityOsirisModStatus.MODFIXER;
								}
								else
								{
									using var stream = f.CreateContentReader();
									using var sr = new StreamReader(stream);
									var text = await sr.ReadToEndAsync();
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
		}

		var metaCount = metaFiles.Count;
		var loadedData = new List<ModData>();

		if (metaCount == 0)
		{
			// Assume it's an override mod since it doesn't have a meta file.
			isOverridingBuiltinDirectory = hasBuiltinDirectory || hasOsirisScripts != DivinityOsirisModStatus.NONE;
		}
		else
		{
			for (var i = 0; i < metaCount; i++)
			{
				var f = metaFiles[i];
				if (f != null)
				{
					var modData = await GetModDataFromMeta(f);
					if (modData != null)
					{
						if (DivinityApp.IgnoredMods.Any(x => x.UUID == modData.UUID))
						{
							modData.IsLarianMod = true;
							modData.SetIsBaseGameMod(true);
						}
						else
						{
							modData.IsUserMod = true;
						}

						modData.OsirisModStatus = hasOsirisScripts;
						modData.Files = files;
						if (isOverridingBuiltinDirectory)
						{
							modData.IsForceLoadedMergedMod = hasModFolderData;
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

						modData.LastModified = fileModified;

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
							var modManagerConfig = await JsonUtils.DeserializeFromAbstractAsync<ModConfig>(modManagerConfigPath, token);
							if (modManagerConfig != null)
							{
								modData.ApplyModConfig(modManagerConfig);
							}
						}

						loadedData.Add(modData);
					}
				}
			}
		}

		if (loadedData.Count == 0)
		{
			if (isOverridingBuiltinDirectory)
			{
				loadedData.Add(new ModData()
				{
					UUID = pakName,
					FilePath = pakPath,
					Name = pakName,
					Folder = builtinModOverrides.FirstOrDefault().Key,
					Description = "This file overrides base game data.",
					ModType = "File Override",
					LastModified = fileModified,
					IsForceLoaded = true,
				});
			}
		}

		return loadedData;
	}

	public static async Task<List<ModData>?> LoadModDataFromPakAsync(string pakPath, Dictionary<string, ModData>? builtinMods, CancellationToken token)
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

	public static async Task<List<ModData>?> LoadModDataFromPakAsync(FileStream stream, string pakPath, Dictionary<string, ModData>? builtinMods, CancellationToken token)
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

	public static async Task<List<ModData>?> LoadModDataFromPakAsync(Package package, Dictionary<string, ModData>? builtinMods, CancellationToken token, bool skipFileParsing = false)
	{
		try
		{
			return await InternalLoadModDataFromPakAsync(package, package.PackagePath, builtinMods, token, skipFileParsing);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak from stream:\n{ex}");
		}
		return null;
	}

	public static async Task<ModSettingsParseResults> LoadModSettingsFileAsync(string path)
	{
		var modOrderUUIDs = new List<string>();
		var activeMods = new List<ModuleShortDesc>();

		if (File.Exists(path))
		{
			Resource? modSettingsRes = null;
			try
			{
				modSettingsRes = await LoadResourceAsync(path, ResourceFormat.LSX, _modSettingsParams);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading '{path}':\n{ex}");
			}

			try
			{
				if (modSettingsRes != null && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
				{
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
									var activeModData = ModuleShortDesc.FromAttributes(c.Attributes);
									if (activeModData?.UUID != null && !ModDataLoader.IgnoreMod(activeModData.UUID))
									{
										activeMods.Add(activeModData);
									}
								}
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				DivinityApp.Log($"Error parsing modsettings '{path}':\n{ex}");
			}
		}

		return new ModSettingsParseResults()
		{
			ActiveMods = activeMods,
		};
	}

	public static async Task<List<ProfileData>> LoadProfileDataAsync(string profilePath)
	{
		List<ProfileData> profiles = [];
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
							name = profileNameAtt.AsString(_lsfSerializationSettings);
						}
						if (region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
						{
							displayedName = profileDisplayNameAtt.AsString(_lsfSerializationSettings);
						}
						if (region.Attributes.TryGetValue("PlayerProfileID", out var profileIdAtt))
						{
							profileUUID = profileIdAtt.AsString(_lsfSerializationSettings);
						}
					}
				}

				if (string.IsNullOrEmpty(name))
				{
					name = folderName;
				}

				if (string.IsNullOrEmpty(displayedName))
				{
					displayedName = name;
				}

				var profileData = new ProfileData()
				{
					Name = name,
					FolderName = folderName,
					ProfileName = displayedName,
					UUID = profileUUID,
					FilePath = Path.GetFullPath(folder)
				};

				var modSettingsFile = Path.Join(folder, "modsettings.lsx");
				var modSettings = await LoadModSettingsFileAsync(modSettingsFile);
				profileData.ActiveMods.AddRange(modSettings.ActiveMods);
				profiles.Add(profileData);
			}
		}
		return profiles;
	}

	public static async Task<Resource?> LoadResourceAsync(string path, ResourceLoadParameters? resourceParams = null)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = ResourceUtils.LoadResource(path, resourceParams ?? _loadParams);
				return resource;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{path}': {ex}");
				return null;
			}
		});
	}

	public static async Task<Resource?> LoadResourceAsync(string path, ResourceFormat resourceFormat, ResourceLoadParameters? resourceParams = null)
	{
		try
		{
			using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			await fs.ReadAsync(new byte[fs.Length], 0, (int)fs.Length);
			fs.Position = 0;
			var resource = ResourceUtils.LoadResource(fs, resourceFormat, resourceParams ?? _loadParams);
			return resource;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading '{path}': {ex}");
			return null;
		}
	}

	public static async Task<Resource?> LoadResourceAsync(System.IO.Stream stream, ResourceFormat resourceFormat, ResourceLoadParameters? resourceParams = null)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = ResourceUtils.LoadResource(stream, resourceFormat, resourceParams ?? _loadParams);
				return resource;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading resource: {ex}");
				return null;
			}
		});
	}

	private static FileInfo? GetProfileFile(string path)
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
		var activeProfileUUID = "";
		if (playerprofilesFile != null)
		{
			DivinityApp.Log($"Loading playerprofiles at '{playerprofilesFile}'");
			var res = await LoadResourceAsync(playerprofilesFile.FullName);
			if (res != null && res.Regions.TryGetValue("UserProfiles", out var region))
			{
				//DivinityApp.LogMessage($"ActiveProfile | Getting root node '{String.Join(";", region.Attributes.Keys)}'");

				if (region.Attributes.TryGetValue("ActiveProfile", out var att))
				{
					activeProfileUUID = att.AsString(_lsfSerializationSettings);
				}
			}
		}
		else
		{
			DivinityApp.Log("No playerprofilesFile found.");
		}
		return activeProfileUUID;
	}

	public static bool ExportLoadOrderToFile(string outputFilePath, ModLoadOrder order)
	{
		var parentDir = Path.GetDirectoryName(outputFilePath);
		if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

		var contents = JsonSerializer.Serialize(order, _indentedJsonSettings);

		FileUtils.WriteTextFile(outputFilePath, contents);

		order.FilePath = outputFilePath;

		return true;
	}

	public static async Task<bool> ExportLoadOrderToFileAsync(string outputFilePath, ModLoadOrder order)
	{
		var parentDir = Path.GetDirectoryName(outputFilePath);
		if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

		var contents = JsonSerializer.Serialize(order, _indentedJsonSettings);

		await FileUtils.WriteTextFileAsync(outputFilePath, contents);

		order.FilePath = outputFilePath;

		return true;
	}

	public static List<ModLoadOrder> FindLoadOrderFilesInDirectory(string directory)
	{
		List<ModLoadOrder> loadOrders = [];

		if (Directory.Exists(directory))
		{
			var files = FileUtils.EnumerateFiles(directory, FileUtils.RecursiveOptions, f => f.EndsWith(".json", SCOMP));

			foreach (var loadOrderFile in files)
			{
				try
				{
					var fileText = File.ReadAllText(loadOrderFile);
					var order = JsonUtils.SafeDeserialize<ModLoadOrder>(fileText);
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

	public static async Task<List<ModLoadOrder>> FindLoadOrderFilesInDirectoryAsync(string directory)
	{
		List<ModLoadOrder> loadOrders = [];

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

					var order = JsonUtils.SafeDeserialize<ModLoadOrder>(fileText);
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
	public static async Task<ModLoadOrder> LoadOrderFromFileAsync(string loadOrderFile)
	{
		if (File.Exists(loadOrderFile))
		{
			try
			{
				using var reader = File.OpenText(loadOrderFile);
				var fileText = await reader.ReadToEndAsync();
				var order = JsonUtils.SafeDeserialize<ModLoadOrder>(fileText);
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

	public static ModLoadOrder LoadOrderFromFile(string loadOrderFile, IEnumerable<ModData> allMods)
	{
		var ext = Path.GetExtension(loadOrderFile).ToLower();
		ModLoadOrder order = null;
		switch (ext)
		{
			case ".json":
				if (JsonUtils.TrySafeDeserializeFromPath<ModLoadOrder>(loadOrderFile, out var savedOrder))
				{
					return savedOrder;
				}
				else
				{
					if (JsonUtils.TrySafeDeserializeFromPath<List<SerializedModData>>(loadOrderFile, out var exportedOrder))
					{
						order = new ModLoadOrder
						{
							IsDecipheredOrder = true
						};
						foreach (var entry in exportedOrder)
						{
							var data = allMods.FirstOrDefault(x => x.UUID == entry.UUID);
							if (data != null)
							{
								order.Add(new ModEntry(data));
							}
							else
							{
								//TODO Missing mod data
							}
						}
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
				order = new ModLoadOrder();
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
							order.Order.Add(new ModLoadOrderEntry
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
					order = new ModLoadOrder();
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
								order.Order.Add(new ModLoadOrderEntry
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

	public static async Task<bool> ExportModSettingsToFileAsync(string folder, IEnumerable<ModData> order)
	{
		if (Directory.Exists(folder))
		{
			var outputFilePath = Path.Join(folder, "modsettings.lsx");
			var contents = GenerateModSettingsFile(order);
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
			settings = JsonUtils.SafeDeserializeFromPath<Dictionary<string, object>>(settingsFilePath);
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

		var contents = JsonSerializer.Serialize(settings, _indentedJsonSettings);

		await FileUtils.WriteTextFileAsync(settingsFilePath, contents);

		DivinityApp.Log($"Updated {settingsFilePath}");
		return true;
	}

	public static List<ModData> GetDependencyMods(ModData mod, IEnumerable<ModData> allMods, HashSet<string> addedMods, bool forExport = true)
	{
		List<ModData> mods = [];
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

	public static List<ModData> BuildOutputList(IEnumerable<ModLoadOrderEntry> order, IEnumerable<ModData> allMods, bool addDependencies = true, ModData selectedAdventure = null)
	{
		List<ModData> orderList = [];
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

	public static string GenerateModSettingsFile(IEnumerable<ModData> orderList)
	{
		/* The "Mods" node is used for the in-game menu it seems. The selected adventure mod is always at the top. */
		var modShortDescText = "";

		foreach (var mod in orderList)
		{
			if (!String.IsNullOrWhiteSpace(mod.UUID))
			{
				//Use Export to support lists with categories/things that don't need exporting
				var modText = mod.Export(ModExportType.XML);
				if (!String.IsNullOrEmpty(modText))
				{
					modShortDescText += modText + Environment.NewLine;
				}
			}
		}

		return String.Format(DivinityApp.XML_MOD_SETTINGS_TEMPLATE, modShortDescText);
	}

	public static string CreateHandle() => Guid.NewGuid().ToString().Replace('-', 'g').Insert(0, "h");

	private static Node? FindNode(Node node, string name)
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

	private static Node? FindNode(Dictionary<string, List<Node>> children, string name)
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

	private static Node? FindNode(Region region, string name)
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

	private static Node? FindNode(Resource resource, string name)
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

	public static ModLoadOrder? GetLoadOrderFromSave(string file, string ordersFolder = "")
	{
		try
		{
			var reader = new PackageReader();
			using var package = reader.Read(file);
			var PackagedFileInfo = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
			if (PackagedFileInfo == null)
			{
				return null;
			}

			Resource? resource = null;

			var rsrcStream = PackagedFileInfo.CreateContentReader();
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
						var orderName = fileName;
						var match = PlayerProfilePathPattern().Match(Path.GetFullPath(file));
						if (match.Success)
						{
							orderName = $"{match.Groups[1].Value}_{fileName}";
						}
						ModLoadOrder loadOrder = new()
						{
							Name = orderName,
							FilePath = Path.Join(ordersFolder, MakeSafeFilename(Path.Join(orderName + ".json"), '_'))
						};

						foreach (var c in modList)
						{
							var name = "";
							string? uuid = null;
							if (c.Attributes.TryGetValue("UUID", out var idAtt))
							{
								uuid = idAtt.AsString(_lsfSerializationSettings);
							}

							if (c.Attributes.TryGetValue("Name", out var nameAtt))
							{
								name = nameAtt.AsString(_lsfSerializationSettings);
							}

							if (uuid != null && !IgnoreMod(uuid))
							{
								DivinityApp.Log($"Found mod in save: '{name}_{uuid}'.");
								loadOrder.Order.Add(new ModLoadOrderEntry()
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

	private readonly static List<string> _fallbackFeatureFlags = [];

	private static async Task<ModScriptExtenderConfig?> LoadScriptExtenderConfigAsync(string configFile)
	{
		try
		{
			using var reader = File.OpenText(configFile);
			var text = await reader.ReadToEndAsync();
			if (!string.IsNullOrWhiteSpace(text) &&
				JsonUtils.TrySafeDeserialize<ModScriptExtenderConfig>(text, out var config))
			{
				return config;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading '{configFile}': {ex}");
		}
		return null;
	}

	private static async Task<ModScriptExtenderConfig?> LoadScriptExtenderConfigAsync(Stream stream)
	{
		try
		{
			using var sr = new StreamReader(stream);
			var text = await sr.ReadToEndAsync();
			if (!string.IsNullOrWhiteSpace(text)
				&& JsonUtils.TrySafeDeserialize<ModScriptExtenderConfig>(text, out var config))
			{
				return config;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading 'ScriptExtenderConfig.json': {ex}");
		}
		return null;
	}

	private static async Task<ModScriptExtenderConfig?> LoadScriptExtenderConfigAsync(PackagedFileInfo configFile)
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

	private static async Task<ModData?> LoadModFromModInfo(string directoryPath, VFS vfs, ModInfo modInfo, bool isGameDirectory, CancellationToken token)
	{
		if (vfs.TryOpen(modInfo.Meta, out var stream))
		{
			using var sr = new StreamReader(stream);
			var text = await sr.ReadToEndAsync(token);
			var modData = ParseMetaFile(text, modInfo.Meta);
			if (modData != null)
			{
				var filePath = modInfo.PackagePath;
				if (string.IsNullOrEmpty(filePath))
				{
					filePath = modInfo.ModsPath;
				}
				else
				{
					try
					{
						modData.LastModified = File.GetLastWriteTime(filePath);
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
						fileTimeFile = Path.GetFullPath(modInfo.Meta, directoryPath);
						modData.IsEditorMod = true;
					}

					try
					{
						modData.LastModified = File.GetLastWriteTime(fileTimeFile);
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
				}
				else
				{
					modData.IsUserMod = true;
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

	public static async Task<ModsLoadingResults> LoadModsAsync(string gameDataPath, string userModsPath, CancellationToken token)
	{
		var time = DateTimeOffset.Now;

		ModDirectoryLoadingResults baseMods = null!;

		if(Directory.Exists(gameDataPath))
		{
			using var dataPakParser = new DirectoryPakParser(gameDataPath, FileUtils.GameDataOptions);
			baseMods = await dataPakParser.ProcessAsync(detectDuplicates: false, parseLooseMetaFiles: true, token);

			DivinityApp.Log($"Took {DateTimeOffset.Now - time:s\\.ff} second(s) to load mods from '{gameDataPath}'");

			time = DateTimeOffset.Now;
		}
		else
		{
			baseMods = new ModDirectoryLoadingResults(gameDataPath);
		}

		foreach (var mod in DivinityApp.IgnoredMods)
		{
			if (!baseMods.Mods.ContainsKey(mod.UUID))
			{
				baseMods.Mods[mod.UUID] = mod;
			}
		}

		using var userPakParser = new DirectoryPakParser(userModsPath, FileUtils.FlatSearchOptions, baseMods.Mods, []);
		var userMods = await userPakParser.ProcessAsync(detectDuplicates: true, parseLooseMetaFiles: false, token);

		DivinityApp.Log($"Took {DateTimeOffset.Now - time:s\\.ff} second(s) to load mods from '{userModsPath}'");

		return new ModsLoadingResults(baseMods, userMods);
	}

	public static ModuleInfo? TryGetMetaFromPakFileStream(FileStream stream, string filePath, CancellationToken token)
	{
		stream.Position = 0;
		var pr = new PackageReader();
		using var pak = pr.Read(filePath, stream);
		if (pak != null && pak.Files != null)
		{
			for (var i = 0; i < pak.Files.Count; i++)
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

	public static async Task ExtractStoryOsiAsync(string pakPath, string outputPath, CancellationToken token)
	{
		var pr = new PackageReader();
		using var pak = pr.Read(pakPath);
		if (pak != null && pak.Files?.Count > 0)
		{
			var osiFile = pak.Files.FirstOrDefault(x => x.Name.EndsWith("story.div.osi")); //Or GustavDev/Story/story.div.osi
			if (osiFile != null)
			{
				using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 32000, true);
				using var osiStream = osiFile.CreateContentReader();
				await osiStream.CopyToAsync(fs, 32000, token);
			}
		}
	}
}
