

using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;

using DynamicData;

using LSLib.LS;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DivinityModManager.Util;

public static partial class DivinityModDataLoader
{
	private static readonly StringComparison SCOMP = StringComparison.OrdinalIgnoreCase;
	private static readonly string[] LarianFileTypes = [".lsb", ".lsf", ".lsx", ".lsj"];

	private static readonly ulong HEADER_MAJOR = 4;
	private static readonly ulong HEADER_MINOR = 8;
	private static readonly ulong HEADER_REVISION = 0;
	private static readonly ulong HEADER_BUILD = 0;

	private static readonly string[] VersionAttributes = ["Version64", "Version"];
	public static readonly HashSet<string> IgnoreBuiltinPath = [];

	private static readonly ResourceLoadParameters _loadParams = ResourceLoadParameters.FromGameVersion(LSLib.LS.Enums.Game.BaldursGate3);
	private static readonly ResourceLoadParameters _modSettingsParams = new() { ByteSwapGuids = false };
	private static readonly NodeSerializationSettings _defaultNodeSettings = new() { ByteSwapGuids = true, DefaultByteSwapGuids = true };

	[GeneratedRegex(@".*PlayerProfiles\\(.*?)\\Savegames.*")]
	private static partial Regex PlayerProfilePathPattern();

	public static bool IgnoreMod(string modUUID)
	{
		return DivinityApp.IgnoredMods.Lookup(modUUID).HasValue;
	}

	public static bool IgnoreModDependency(string modUUID)
	{
		return DivinityApp.IgnoredDependencyMods.Contains(modUUID) || IgnoreMod(modUUID);
	}

	public static bool IgnoreModByFolder(string folder)
	{
		return DivinityApp.IgnoredMods.Items.Any(m => m.Folder.Equals(Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar)), SCOMP));
	}

	public static string MakeSafeFilename(string filename, char replaceChar)
	{
		foreach (char c in Path.GetInvalidFileNameChars())
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
	private static string GetAttributeValueWithId(XElement node, string id, string fallbackValue = "")
	{
		var value = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
		if (value != null)
		{
			return value;
		}
		return fallbackValue;
	}

	private static string GetAttributeValueWithId(XElement node, string[] ids, string fallbackValue = "")
	{
		foreach (var id in ids)
		{
			var value = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
			if (value != null)
			{
				return value;
			}
		}
		return fallbackValue;
	}

	private static ulong GetAttributeValueWithId(XElement node, string id, ulong fallbackValue = 0ul)
	{
		var attValue = node.Descendants("attribute").FirstOrDefault(a => a.Attribute("id")?.Value == id)?.Attribute("value")?.Value;
		if (attValue != null && ulong.TryParse(attValue, out var value))
		{
			return value;
		}
		return fallbackValue;
	}

	private static bool TryGetNodeValue(XElement node, string id, out string value, string fallbackValue = "")
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
			xmlstring = Regex.Replace(xmlstring, "value=\"(.*?)\"", new MatchEvaluator((m) =>
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

	private static DivinityModData ParseMetaFile(string metaContents, bool isBaseGameMod = false)
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
				if (TryGetNodeValue(versionNode, "major", out var headerMajorStr))
				{
					ulong.TryParse(headerMajorStr, out headerMajor);
				}
				if (TryGetNodeValue(versionNode, "minor", out var headerMinorStr))
				{
					ulong.TryParse(headerMinorStr, out headerMinor);
				}
				if (TryGetNodeValue(versionNode, "revision", out var headerRevisionStr))
				{
					ulong.TryParse(headerRevisionStr, out headerRevision);
				}
				if (TryGetNodeValue(versionNode, "build", out var headerBuildStr))
				{
					ulong.TryParse(headerBuildStr, out headerBuild);
				}

				//DivinityApp.LogMessage($"Version: {headerMajor}.{headerMinor}.{headerRevision}.{headerBuild}");
			}

			var moduleInfoNode = xDoc.Descendants("node").FirstOrDefault(n => n.Attribute("id")?.Value == "ModuleInfo");
			if (moduleInfoNode != null)
			{
				var uuid = GetAttributeValueWithId(moduleInfoNode, "UUID", "");
				var name = UnescapeXml(GetAttributeValueWithId(moduleInfoNode, "Name", ""));
				var description = UnescapeXml(GetAttributeValueWithId(moduleInfoNode, "Description", ""));
				var author = UnescapeXml(GetAttributeValueWithId(moduleInfoNode, "Author", ""));
				/*
				if (DivinityApp.MODS_GiftBag.Any(x => x.UUID == uuid))
				{
					name = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DisplayName", name));
					description = UnescapeXml(GetAttributeWithId(moduleInfoNode, "DescriptionName", description));
					author = "Larian Studios";
				}
				*/

				DivinityModData modData = new DivinityModData(isBaseGameMod)
				{
					HasMetadata = true,
					UUID = uuid,
					Name = name,
					Author = author,
					Version = DivinityModVersion2.FromInt(SafeConvertStringUnsigned(GetAttributeValueWithId(moduleInfoNode, VersionAttributes, ""))),
					Folder = GetAttributeValueWithId(moduleInfoNode, "Folder", ""),
					Description = description,
					MD5 = GetAttributeValueWithId(moduleInfoNode, "MD5", ""),
					ModType = GetAttributeValueWithId(moduleInfoNode, "Type", ""), // Deprecated?
					PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
					FileSize = GetAttributeValueWithId(moduleInfoNode, "FileSize", 0ul),
					HeaderVersion = new DivinityModVersion2(headerMajor, headerMinor, headerRevision, headerBuild)
				};

				//GustavDev, GustavX
				if (uuid == DivinityApp.GUSTAVDEV_UUID || uuid == DivinityApp.GUSTAVX_UUID)
				{
					modData.ModType = "Adventure";
				}

				var tagsText = GetAttributeValueWithId(moduleInfoNode, "Tags", "");
				if (!String.IsNullOrWhiteSpace(tagsText))
				{
					var tags = tagsText.Split(';');
					modData.AddTags(tags);
				}

				var dependenciesRoot = xDoc.Descendants("node").FirstOrDefault(x => x.Attribute("id")?.Value == "Dependencies");

				if (dependenciesRoot != null)
				{
					var dependenciesNodes = dependenciesRoot.Descendants("node").Where(n => n.Attribute("id")?.Value == "ModuleShortDesc");

					if (dependenciesNodes != null)
					{
						foreach (var node in dependenciesNodes)
						{
							var entryUUID = GetAttributeValueWithId(node, "UUID", "");
							if (!String.IsNullOrWhiteSpace(entryUUID))
							{
								modData.Dependencies.AddOrUpdate(new ModuleShortDesc()
								{
									Folder = GetAttributeValueWithId(node, "Folder", ""),
									MD5 = GetAttributeValueWithId(node, "MD5", ""),
									Name = UnescapeXml(GetAttributeValueWithId(node, "Name", "")),
									PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
									UUID = GetAttributeValueWithId(node, "UUID", ""),
									Version = DivinityModVersion2.FromInt(SafeConvertStringUnsigned(GetAttributeValueWithId(node, VersionAttributes, ""))),
								});
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
							var entryUUID = GetAttributeValueWithId(node, "UUID", "");
							if (!String.IsNullOrWhiteSpace(entryUUID))
							{
								modData.Conflicts.AddOrUpdate(new ModuleShortDesc()
								{
									Folder = GetAttributeValueWithId(node, "Folder", ""),
									MD5 = GetAttributeValueWithId(node, "MD5", ""),
									Name = UnescapeXml(GetAttributeValueWithId(node, "Name", "")),
									PublishHandle = GetAttributeValueWithId(moduleInfoNode, "PublishHandle", 0ul),
									UUID = GetAttributeValueWithId(node, "UUID", ""),
									Version = DivinityModVersion2.FromInt(SafeConvertStringUnsigned(GetAttributeValueWithId(node, VersionAttributes, ""))),
								});
							}
						}
					}
				}

				var publishVersionNode = moduleInfoNode.Descendants("node").Where(n => n.Attribute("id")?.Value == "PublishVersion").FirstOrDefault();
				if (publishVersionNode != null)
				{
					var publishVersion = DivinityModVersion2.FromInt(SafeConvertStringUnsigned(GetAttributeValueWithId(publishVersionNode, VersionAttributes, "")));
					modData.PublishVersion = publishVersion;
					//DivinityApp.LogMessage($"{modData.Folder} PublishVersion is {publishVersion.Version}");
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
				System.IO.FileMode.Open,
				System.IO.FileAccess.Read,
				System.IO.FileShare.Read,
				2048,
				System.IO.FileOptions.Asynchronous);
	}

	private static async Task<DivinityModData> LoadEditorProjectFolderAsync(string folder, CancellationToken token)
	{
		var metaFile = Path.Combine(folder, "meta.lsx");
		if (File.Exists(metaFile))
		{
			using var fileStream = GetAsyncStream(metaFile);
			var result = new byte[fileStream.Length];
			await fileStream.ReadAsync(result, 0, (int)fileStream.Length, token);

			string str = Encoding.UTF8.GetString(result, 0, result.Length);

			if (!String.IsNullOrEmpty(str))
			{
				//XML parsing doesn't like the BOM for some reason
				if (str.StartsWith(_byteOrderMarkUtf8, StringComparison.Ordinal))
				{
					str = str.Remove(0, _byteOrderMarkUtf8.Length);
				}

				DivinityModData modData = ParseMetaFile(str);
				if (modData != null)
				{
					modData.IsEditorMod = true;
					modData.IsUserMod = true;
					modData.FilePath = folder;
					try
					{
						modData.LastModified = File.GetLastWriteTime(metaFile);
						modData.LastUpdated = modData.LastModified.Value;
					}
					catch (PlatformNotSupportedException ex)
					{
						DivinityApp.Log($"Error getting last modified date for '{metaFile}': {ex}");
					}

					var extenderConfigPath = Path.Combine(folder, DivinityApp.EXTENDER_MOD_CONFIG);
					if (File.Exists(extenderConfigPath))
					{
						var extenderConfig = await LoadScriptExtenderConfigAsync(extenderConfigPath);
						if (extenderConfig != null)
						{
							modData.ScriptExtenderData = extenderConfig;
							if (modData.ScriptExtenderData.RequiredVersion > -1) modData.HasScriptExtenderSettings = true;
						}
						else
						{
							DivinityApp.Log($"Failed to parse {DivinityApp.EXTENDER_MOD_CONFIG} for '{folder}'.");
						}
					}

					return modData;
				}
			}
		}
		return null;
	}

	public static async Task<List<DivinityModData>> LoadEditorProjectsAsync(string modsFolderPath, CancellationToken token)
	{
		var projects = new ConcurrentBag<DivinityModData>();

		try
		{
			if (Directory.Exists(modsFolderPath))
			{
				var projectDirectories = Directory.EnumerateDirectories(modsFolderPath);
				var filteredFolders = projectDirectories.Where(f => !IgnoreModByFolder(f));
				Console.WriteLine($"Project Folders: {filteredFolders.Count()} / {projectDirectories.Count()}");

				var currentTime = DateTime.Now;
				foreach(var folder in filteredFolders)
				{
					if (token.IsCancellationRequested) break;
					var modData = await LoadEditorProjectFolderAsync(folder, token);
					if (modData != null)
					{
						projects.Add(modData);
					}
				}
				DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} seconds(s) to load editor mods.");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod projects: {ex}");
		}
		return projects.ToList();
	}

	private static readonly HashSet<string> _AllPaksNames = new();
	private static readonly Regex multiPartPakPatternNoExtension = new("(_[0-9]+)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);

	private static bool PakIsNotPartial(string path)
	{
		var baseName = Path.GetFileNameWithoutExtension(path);
		var match = multiPartPakPatternNoExtension.Match(baseName);
		if (match.Success)
		{
			var nameWithoutPartial = baseName.Replace(match.Groups[0].Value, "");
			if (_AllPaksNames.Contains(nameWithoutPartial))
			{
				DivinityApp.Log($"Pak ({baseName}) is a partial pak for ({nameWithoutPartial}). Skipping.");
				return false;
			}
		}
		return true;
	}

	private static readonly Regex modMetaPattern = new("^Mods/([^/]+)/meta.lsx", RegexOptions.IgnoreCase);
	private static bool IsModMetaFile(PackagedFileInfo f)
	{
		if (Path.GetFileName(f.Name).Equals("meta.lsx", SCOMP))
		{
			return modMetaPattern.IsMatch(f.Name);
		}
		return false;
	}

	private static readonly Regex _ModFolderPattern = new("^(Mods|Public)/(.+?)/.+$");
	private static readonly string[] _IgnoredRecursiveFolders = new string[]
	{
		"Baldur's Gate 3\\Data",
		"Baldur's Gate 3\\bin",
		"Localization",
	};

	private static async Task<DivinityModData> InternalLoadModDataFromPakAsync(Package pak, string pakPath, Dictionary<string, DivinityModData> builtinMods)
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

		if (pak != null && pak.Files != null)
		{
			for (int i = 0; i < pak.Files.Count; i++)
			{
				var f = pak.Files[i];
				files.Add(f.Name);

				if (f.Name.Contains(DivinityApp.EXTENDER_MOD_CONFIG))
				{
					extenderConfigPath = f;
				}
				else if (IsModMetaFile(f))
				{
					metaFiles.Add(f);
				}
				else
				{
					var modFolderMatch = _ModFolderPattern.Match(f.Name);
					if (modFolderMatch.Success)
					{
						var modFolder = Path.GetFileName(modFolderMatch.Groups[2].Value.TrimEnd(Path.DirectorySeparatorChar));
						if (f.Name.Contains($"Mods/{modFolder}/Story/RawFiles/Goals"))
						{
							if (hasOsirisScripts == DivinityOsirisModStatus.NONE)
							{
								hasOsirisScripts = DivinityOsirisModStatus.SCRIPTS;
							}
							if (f.Name.Contains("ForceRecompile.txt"))
							{
								hasOsirisScripts = DivinityOsirisModStatus.MODFIXER;
							}
							else
							{
								using var stream = f.CreateContentReader();
								using var sr = new System.IO.StreamReader(stream);
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
			using (var stream = metaFile.CreateContentReader())
			{
				using var sr = new System.IO.StreamReader(stream);
				string text = await sr.ReadToEndAsync();
				modData = ParseMetaFile(text);
			}
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
				FilePath = pakPath,
				Name = Path.GetFileNameWithoutExtension(pakPath),
				Folder = builtinModOverrides.FirstOrDefault().Key,
				Description = "This file overrides base game data.",
				ModType = "File Override",
				UUID = pakPath,
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

	private static async Task<DivinityModData> LoadModDataFromPakReaderAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods, FileStream stream = null)
	{
		var pr = new PackageReader();
		using var pak = stream == null ? pr.Read(pakPath) : pr.Read(pakPath, stream);
		return await InternalLoadModDataFromPakAsync(pak, pakPath, builtinMods);
	}

	public static async Task LoadModDataFromPakToBagAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods, ConcurrentBag<DivinityModData> targetBag, CancellationToken cts)
	{
		try
		{
			if (cts.IsCancellationRequested) return;
			var result = await LoadModDataFromPakReaderAsync(pakPath, builtinMods);
			if (result != null)
			{
				targetBag.Add(result);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak '{pakPath}':\n{ex}");
		}
	}

	public static async Task LoadModDataFromPakToBagAsync(FileStream stream, string pakPath, Dictionary<string, DivinityModData> builtinMods, ConcurrentBag<DivinityModData> targetBag, CancellationToken cts)
	{
		try
		{
			if (cts.IsCancellationRequested) return;
			stream.Position = 0;
			var result = await LoadModDataFromPakReaderAsync(pakPath, builtinMods, stream);
			if (result != null)
			{
				targetBag.Add(result);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak from stream:\n{ex}");
		}
	}

	public static async Task<DivinityModData> LoadModDataFromPakAsync(string pakPath, Dictionary<string, DivinityModData> builtinMods, CancellationToken cts, FileStream stream = null)
	{
		try
		{
			if (cts.IsCancellationRequested) return null;
			return await LoadModDataFromPakReaderAsync(pakPath, builtinMods, stream);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading mod pak '{pakPath}':\n{ex}");
		}
		return null;
	}

	public static async Task<ModLoadingResults> LoadModPackageDataAsync(string modsFolderPath, CancellationToken cts)
	{
		var builtinMods = DivinityApp.IgnoredMods.Items.SafeToDictionary(x => x.Folder, x => x);

		var results = new ModLoadingResults()
		{
			DirectoryPath = modsFolderPath
		};

		var modPaks = new List<string>();
		try
		{
			// Inactive packages remain available to the manager, outside the game's scan.
			var allPaks = ModPackageIsolation.GetPackageDirectories(modsFolderPath)
				.SelectMany(folder => DivinityFileUtils.EnumerateFiles(folder, DivinityFileUtils.RecursiveOptions,
			(f) =>
			{
				var name = Path.GetFileName(f);
				return name.EndsWith(".pak", SCOMP) && !_IgnoredRecursiveFolders.Any(x => f.Contains(x));
			})).ToList();
			_AllPaksNames.Clear();
			_AllPaksNames.UnionWith(allPaks.Select(p => Path.GetFileNameWithoutExtension(p)));
			modPaks.AddRange(allPaks.Where(PakIsNotPartial));
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error enumerating pak folder '{modsFolderPath}': {ex}");
		}

		DivinityApp.Log($"Mod Packages: {modPaks.Count}");

		var loadedMods = new ConcurrentBag<DivinityModData>();

		var currentTime = DateTime.Now;

		var tasks = new List<Task>();
		foreach (var pak in modPaks)
		{
			if (cts.IsCancellationRequested) break;
			tasks.Add(LoadModDataFromPakToBagAsync(pak, builtinMods, loadedMods, cts));
		}

		await Task.WhenAll(tasks);

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

	private static Node FindResourceNode(Node node, string attribute, string matchVal)
	{
		if (node.Attributes.TryGetValue(attribute, out var att))
		{
			var attVal = att.AsString(_defaultNodeSettings);
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
		var activeMods = new List<DivinityProfileActiveModData>();

		if (File.Exists(path))
		{
			Resource modSettingsRes = null;
			try
			{
				modSettingsRes = await LoadResourceAsync(path, LSLib.LS.Enums.ResourceFormat.LSX, _modSettingsParams);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading '{path}':\n{ex}");
			}

			if (modSettingsRes != null && modSettingsRes.Regions.ContainsKey("ModuleSettings") && modSettingsRes.Regions.TryGetValue("ModuleSettings", out var region))
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
			ActiveMods = activeMods,
		};
	}

	public static async Task<List<DivinityProfileData>> LoadProfileDataAsync(string profilePath)
	{
		List<DivinityProfileData> profiles = new List<DivinityProfileData>();
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
							name = profileNameAtt.AsString(_defaultNodeSettings);
						}
						if (region.Attributes.TryGetValue("PlayerProfileDisplayName", out var profileDisplayNameAtt))
						{
							displayedName = profileDisplayNameAtt.AsString(_defaultNodeSettings);
						}
						if (region.Attributes.TryGetValue("PlayerProfileID", out var profileIdAtt))
						{
							profileUUID = profileIdAtt.AsString(_defaultNodeSettings);
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

				var modSettingsFile = Path.Combine(folder, "modsettings.lsx");
				try
				{
					var profileData = new DivinityProfileData(profileUUID, modSettingsFile)
					{
						Name = name,
						ProfileName = displayedName
					};

					var modSettings = await LoadModSettingsFileAsync(modSettingsFile);
					await Observable.Start(() =>
					{
						profileData.ActiveMods.AddRange(modSettings.ActiveMods);
						profiles.Add(profileData);
					}, RxApp.MainThreadScheduler);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error parsing profile modsettings.lsx:\n{ex}");
				}
			}
		}
		return profiles;
	}

	public static async Task<Resource> LoadResourceAsync(string path, ResourceLoadParameters resourceParams = null)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = LSLib.LS.ResourceUtils.LoadResource(path, resourceParams ?? _loadParams);
				return resource;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{path}': {ex}");
				return null;
			}
		});
	}

	public static async Task<Resource> LoadResourceAsync(string path, LSLib.LS.Enums.ResourceFormat resourceFormat, ResourceLoadParameters resourceParams = null)
	{
		try
		{
			using var fs = File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
			await fs.ReadAsync(new byte[fs.Length], 0, (int)fs.Length);
			fs.Position = 0;
			var resource = LSLib.LS.ResourceUtils.LoadResource(fs, resourceFormat, resourceParams ?? _loadParams);
			return resource;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading '{path}': {ex}");
			return null;
		}
	}

	public static async Task<Resource> LoadResourceAsync(System.IO.Stream stream, LSLib.LS.Enums.ResourceFormat resourceFormat)
	{
		return await Task.Run(() =>
		{
			try
			{
				var resource = LSLib.LS.ResourceUtils.LoadResource(stream, resourceFormat, _loadParams);
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
		var files = DivinityFileUtils.EnumerateFiles(path, null, (f) =>
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
		var files = DivinityFileUtils.EnumerateFiles(path, null, (f) =>
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
				if (region.Attributes.TryGetValue("ActiveProfile", out var att))
				{
					activeProfileUUID = att.AsString(_defaultNodeSettings);
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

		var buffer = Encoding.UTF8.GetBytes(contents);
		using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, false))
		{
			fs.Write(buffer, 0, buffer.Length);
		}

		order.FilePath = outputFilePath;

		return true;
	}

	public static async Task<bool> ExportLoadOrderToFileAsync(string outputFilePath, DivinityLoadOrder order)
	{
		var parentDir = Path.GetDirectoryName(outputFilePath);
		if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

		string contents = JsonConvert.SerializeObject(order, Newtonsoft.Json.Formatting.Indented);

		var buffer = Encoding.UTF8.GetBytes(contents);
		using (var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
		{
			await fs.WriteAsync(buffer);
		}

		order.FilePath = outputFilePath;

		return true;
	}

	public static List<DivinityLoadOrder> FindLoadOrderFilesInDirectory(string directory)
	{
		List<DivinityLoadOrder> loadOrders = [];

		if (Directory.Exists(directory))
		{
			var files = DivinityFileUtils.EnumerateFiles(directory, DivinityFileUtils.RecursiveOptions, f => f.EndsWith(".json", SCOMP));

			foreach (var loadOrderFile in files)
			{
				try
				{
					var fileText = File.ReadAllText(loadOrderFile);
					var order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
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
		List<DivinityLoadOrder> loadOrders = [];

		if (Directory.Exists(directory))
		{
			var options = new EnumerationOptions()
			{
				RecurseSubdirectories = true
			};
			var files = DivinityFileUtils.EnumerateFiles(directory, options,
				(f) => f.EndsWith(".json", SCOMP) && !f.Equals("settings.json", SCOMP));

			foreach (var loadOrderFile in files)
			{
				try
				{
					using var reader = File.OpenText(loadOrderFile);
					var fileText = await reader.ReadToEndAsync();

					var order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(fileText);
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

	public static async Task<bool> ExportModSettingsToFileAsync(string folder, IEnumerable<DivinityModData> order,
		string modsFolderPath = null, IEnumerable<DivinityModData> allMods = null)
	{
		if (Directory.Exists(folder))
		{
			string outputFilePath = Path.Combine(folder, "modsettings.lsx");
			string contents = GenerateModSettingsFile(order);
			try
			{
				if (allMods != null)
				{
					var storage = ModPackageIsolation.GetInactiveDirectory(modsFolderPath);
					var folders = ModPackageIsolation.GetPackageDirectories(modsFolderPath).ToArray();
					var available = allMods.Where(m => !m.IsEditorMod && !m.IsLarianMod && m.CanAddToLoadOrder
						&& !string.IsNullOrEmpty(m.FilePath) && folders.Any(f => DivinityFileUtils.IsSubdirectoryOf(f, m.FilePath))).ToList();
					var active = order.Select(m => m.UUID).ToHashSet(StringComparer.OrdinalIgnoreCase);
					var moves = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
					foreach (var group in available.GroupBy(m => m.FilePath, StringComparer.OrdinalIgnoreCase))
					{
						var sourceRoot = folders.First(f => DivinityFileUtils.IsSubdirectoryOf(f, group.Key));
						var targetRoot = group.Any(m => active.Contains(m.UUID)) ? modsFolderPath : storage;
						using var pak = new PackageReader().Read(group.Key, metadataOnly: true);
						for (var part = 0; part < Math.Max(1, (int)pak.Metadata.NumParts); part++)
						{
							var source = part == 0 ? group.Key : Package.MakePartFilename(group.Key, part);
							var target = Path.Combine(targetRoot, Path.GetRelativePath(sourceRoot, source));
							moves.Add(source, target);
						}
					}
					var exported = await ModPackageIsolation.ExportAsync(moves, () => ExportModSettingsToFileAsync(folder, order));
					if (exported)
						foreach (var mod in available)
							if (moves.TryGetValue(mod.FilePath, out var target)) mod.FilePath = target;
					return exported;
				}
				//Lazy indentation!
				var xml = new XmlDocument();
				xml.LoadXml(contents);
				using var sw = new StringWriter();
				using var xw = new XmlTextWriter(sw);
				xw.Formatting = System.Xml.Formatting.Indented;
				xw.Indentation = 2;
				xml.WriteTo(xw);

				var buffer = Encoding.UTF8.GetBytes(sw.ToString());
				using var fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true);
				await fs.WriteAsync(buffer);

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
		var folderDir = Path.Combine(appDataLarianFolder, @"Launcher\Settings");
		var settingsFilePath = Path.Combine(folderDir, "preferences.json");
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

		var buffer = Encoding.UTF8.GetBytes(contents);
		using var fs = new System.IO.FileStream(settingsFilePath, System.IO.FileMode.Create,
			System.IO.FileAccess.Write, System.IO.FileShare.None, buffer.Length, true);
		await fs.WriteAsync(buffer, 0, buffer.Length);
		DivinityApp.Log($"Updated {settingsFilePath}");
		return true;
	}

	public static List<DivinityModData> GetDependencyMods(DivinityModData mod, IEnumerable<DivinityModData> allMods, HashSet<string> addedMods)
	{
		List<DivinityModData> mods = new List<DivinityModData>();
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
						if (!addedMods.Contains(m.UUID))
						{
							addedMods.Add(m.UUID);
							mods.Add(m);
						}
					}
				}
				if (!addedMods.Contains(dependencyModData.UUID))
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
		List<DivinityModData> orderList = new List<DivinityModData>();
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

	public static string GenerateModSettingsFile(IEnumerable<DivinityModData> orderList)
	{
		/* The "Mods" node is used for the in-game menu it seems. The selected adventure mod is always at the top. */
		string modShortDescText = "";

		foreach (var mod in orderList)
		{
			if (!String.IsNullOrWhiteSpace(mod.UUID))
			{
				string safeName = System.Security.SecurityElement.Escape(mod.Name);
				// MD5 doesn't seem to actually be used
				modShortDescText += String.Format(DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5 ?? "", safeName, mod.UUID, mod.Version.VersionInt, mod.PublishHandle) + Environment.NewLine;
			}
		}

		return String.Format(DivinityApp.XML_MOD_SETTINGS_TEMPLATE, modShortDescText);
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
			var abstractFileInfo = package.Files.FirstOrDefault(p => p.Name == "meta.lsf");
			if (abstractFileInfo == null)
			{
				return null;
			}

			Resource resource;
			try
			{
				using var rsrcStream = abstractFileInfo.CreateContentReader();
				using var rsrcReader = new LSFReader(rsrcStream);
				resource = rsrcReader.Read();
			}
			finally { }

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
						var match = PlayerProfilePathPattern().Match(Path.GetFullPath(file));
						if (match.Success)
						{
							orderName = $"{match.Groups[1].Value}_{fileName}";
						}
						DivinityLoadOrder loadOrder = new DivinityLoadOrder()
						{
							Name = orderName,
							FilePath = Path.Combine(ordersFolder, MakeSafeFilename(Path.Combine(orderName + ".json"), '_'))
						};

						foreach (var c in modList)
						{
							string name = "";
							string uuid = null;
							if (c.Attributes.TryGetValue("UUID", out var idAtt))
							{
								uuid = idAtt.AsString(_defaultNodeSettings);
							}

							if (c.Attributes.TryGetValue("Name", out var nameAtt))
							{
								name = nameAtt.AsString(_defaultNodeSettings);
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

	private static async Task<DivinityModScriptExtenderConfig> LoadScriptExtenderConfigAsync(PackagedFileInfo configFile)
	{
		try
		{
			using var stream = configFile.CreateContentReader();
			using var sr = new System.IO.StreamReader(stream);
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

	private static async Task<DivinityModData?> LoadModFromModInfo(string directoryPath, VFS vfs, ModInfo modInfo, bool isGameDirectory, CancellationToken token)
	{
		if (vfs.TryOpen(modInfo.Meta, out var stream))
		{
			using var sr = new System.IO.StreamReader(stream);
			var text = await sr.ReadToEndAsync(token);
			var modData = ParseMetaFile(text, false);
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
					modData.FilePath = filePath.Replace('/', Path.DirectorySeparatorChar);
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

				modData.IsLarianMod = modData.Author.Contains("Larian") || (String.IsNullOrEmpty(modData.Author) && !modData.IsEditorMod);
				modData.IsHidden = modData.IsLarianMod && modData.UUID != DivinityApp.MAIN_CAMPAIGN_UUID;

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
			using var vfs = new VFS();
			vfs.AttachGameDirectory(gameDataPath);
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

			modHelper.DiscoverMods();

			if (modResources.Mods != null && modResources.Mods.Values != null)
			{
				var currentTime = DateTime.Now;
				foreach (var modInfo in modResources.Mods.Values)
				{
					var modData = await LoadModFromModInfo(gameDataPath, vfs, modInfo, true, token);
					if (modData != null)
					{
						baseMods.Add(modData);
					}
				}
				DivinityApp.Log($"Took {DateTime.Now - currentTime:s\\.ff} seconds(s) to load builtin mods ({String.Join(";", modResources.Mods.Select(x => x.Value.Name))}).");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error parsing base game mods:\n" + ex.ToString());
		}

		return baseMods;
	}
}
