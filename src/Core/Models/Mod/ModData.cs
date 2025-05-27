using DynamicData;
using DynamicData.Binding;

using ModManager.Models.GitHub;
using ModManager.Models.Mod.Game;
using ModManager.Models.Modio;
using ModManager.Models.NexusMods;
using ModManager.Util;

using System.Globalization;
using System.Runtime.Serialization;

namespace ModManager.Models.Mod;

[DataContract]
[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
public class ModData : ReactiveObject, IModData
{
	#region meta.lsx Properties
	[Reactive, DataMember] public string UUID { get; set; }
	[Reactive, DataMember] public string? Folder { get; set; }
	[Reactive, DataMember] public string? Name { get; set; }
	[Reactive, DataMember] public string? Description { get; set; }
	[Reactive, DataMember] public string? Author { get; set; }
	[Reactive, DataMember] public string? ModType { get; set; }
	[Reactive] public string? MD5 { get; set; }
	[Reactive, DataMember] public LarianVersion Version { get; set; }
	[Reactive, DataMember] public ulong PublishHandle { get; set; }
	[Reactive, DataMember] public ulong FileSize { get; set; }
	[Reactive] public LarianVersion HeaderVersion { get; set; }
	[Reactive] public LarianVersion PublishVersion { get; set; }
	public List<string> Tags { get; set; } = [];

	public SourceCache<ModuleShortDesc, string> Dependencies { get; }
	public SourceCache<ModuleShortDesc, string> MissingDependencies { get; }
	public SourceCache<ModuleShortDesc, string> Conflicts { get; }

	#endregion

	[Reactive] public DateTimeOffset? LastModified { get; set; }

	[Reactive] public bool DisplayFileForName { get; set; }
	[Reactive] public bool IsHidden { get; set; }

	/// <summary>True if this mod is in DivinityApp.IgnoredMods, or the author is Larian. Larian mods are hidden from the load order.</summary>
	[Reactive] public bool IsLarianMod { get; set; }

	/// <summary>Whether the mod was loaded from the user's mods directory.</summary>
	[Reactive] public bool IsUserMod { get; set; }

	/// <summary>
	/// True if the mod has a meta.lsx.
	/// </summary>
	[Reactive] public bool HasMetadata { get; set; }

	/// <summary>True if the mod has a base game mod directory. This data is always loaded regardless if the mod is enabled or not.</summary>
	[Reactive] public bool IsForceLoaded { get; set; }
	/// <summary>
	/// Whether the mod has files of its own (i.e. it overrides Gustav, but it has Public/ModFolder/Assets files etc).
	/// </summary>
	[Reactive] public bool IsForceLoadedMergedMod { get; set; }

	/// <summary>
	/// For situations where an override pak has a meta.lsx with no original files, but it needs to be allowed in the load order anyway.
	/// </summary>
	[Reactive] public bool ForceAllowInLoadOrder { get; set; }
	[Reactive] public string? BuiltinOverrideModsText { get; set; }

	[Reactive] public string? HelpText { get; set; }

	[Reactive] public bool IsVisible { get; set; }

	[Reactive] public int Index { get; set; }

	[Reactive] public ModExtenderStatus ExtenderModStatus { get; set; }
	[Reactive] public DivinityOsirisModStatus OsirisModStatus { get; set; }

	[Reactive] public int CurrentExtenderVersion { get; set; }

	[Reactive] public ModScriptExtenderConfig? ScriptExtenderData { get; set; }


	protected ReadOnlyObservableCollection<ModuleShortDesc> _displayedDependencies;
	public ReadOnlyObservableCollection<ModuleShortDesc> DisplayedDependencies => _displayedDependencies;

	protected ReadOnlyObservableCollection<ModuleShortDesc> _displayedConflicts;
	public ReadOnlyObservableCollection<ModuleShortDesc>DisplayedConflicts => _displayedConflicts;

	[ObservableAsProperty] public int TotalDependencies { get; }
	[ObservableAsProperty] public int TotalConflicts { get; }

	[Reactive] public bool HasScriptExtenderSettings { get; set; }
	[Reactive] public bool IsActive { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsDraggable { get; set; }

	[Reactive] public bool IsLooseMod { get; set; }

	[Reactive] public bool IsToolkitProject { get; set; }
	[Reactive] public ToolkitProjectMetaData? ToolkitProjectMeta { get; set; }

	public string? OutputPakName
	{
		get
		{
			if (UUID.IsValid() && Folder?.Contains(UUID) == false)
			{
				return $"{Folder}_{UUID}.pak";
			}
			else if (FileName.IsValid())
			{
				return $"{FileName}.pak";
			}
			return "";
		}
	}

	[Reactive] public string? FilePath { get; set; }

	//These properties may be accessed from code, so they need BindTo in order to be updated as soon as possible.
	[Reactive] public string? FileName { get; private set; }
	[Reactive] public string? AuthorDisplayName { get; private set; }

	// This is a property instead of an ObservableAsProperty so the name is set immediately
	[Reactive] public string? DisplayName { get; private set; }

	[ObservableAsProperty] public string? ForceAllowInLoadOrderLabel { get; }
	[ObservableAsProperty] public string? ToggleModNameLabel { get; }
	[ObservableAsProperty] public string? DisplayVersion { get; }
	[ObservableAsProperty] public string? ModDisplayTypeName { get; }
	[ObservableAsProperty] public string? ModDisplayTypeForeground { get; }
	[ObservableAsProperty] public string? LastModifiedDateText { get; }
	[ObservableAsProperty] public string? Notes { get; }
	[ObservableAsProperty] public string? OsirisStatusToolTipText { get; }
	[ObservableAsProperty] public string? ScriptExtenderSupportToolTipText { get; }

	[ObservableAsProperty] public bool CanAddToLoadOrder { get; }
	[ObservableAsProperty] public bool CanDelete { get; }
	[ObservableAsProperty] public bool CanForceAllowInLoadOrder { get; }
	[ObservableAsProperty] public bool HasAnyExternalLink { get; }
	[ObservableAsProperty] public bool HasAuthor { get; }
	[ObservableAsProperty] public bool HasDescription { get; }
	[ObservableAsProperty] public bool HasExtenderStatus { get; }
	[ObservableAsProperty] public bool HasFilePath { get; }
	[ObservableAsProperty] public bool HasGitHubLink { get; }
	[ObservableAsProperty] public bool HasModioLink { get; }
	[ObservableAsProperty] public bool HasNexusModsLink { get; }
	[ObservableAsProperty] public bool HasNotes { get; }
	[ObservableAsProperty] public bool HasOsirisStatus { get; }
	[ObservableAsProperty] public bool HasDependencies { get; }
	[ObservableAsProperty] public bool HasConflicts { get; }
	[ObservableAsProperty] public bool HasToolTip { get; }
	[ObservableAsProperty] public bool HasInvalidUUID { get; }
	[ObservableAsProperty] public bool HasMissingDependency { get; }
	[ObservableAsProperty] public string? MissingDependencyToolTip { get; }
	[ObservableAsProperty] public bool HasToolkitIcon { get; }

	[ObservableAsProperty] public ScriptExtenderIconType ExtenderIcon { get; }

	#region NexusMods Properties
	[ObservableAsProperty] public bool NexusImageVisibility { get; }
	[ObservableAsProperty] public bool NexusModsInformationVisibility { get; }
	[ObservableAsProperty] public DateTime NexusModsCreatedDate { get; }
	[ObservableAsProperty] public DateTime NexusModsUpdatedDate { get; }
	[ObservableAsProperty] public string? NexusModsTooltipInfo { get; }

	#endregion

	[Reactive] public bool GitHubEnabled { get; set; }
	[Reactive] public bool NexusModsEnabled { get; set; }
	[Reactive] public bool ModioEnabled { get; set; }
	[Reactive] public bool CanDrag { get; set; }
	[Reactive] public bool HasColorOverride { get; set; }
	[Reactive] public bool DisplayExtraIcons { get; set; }
	[Reactive] public string? SelectedColor { get; set; }
	[Reactive] public string? PointerOverColor { get; set; }
	[Reactive] public string? ListColor { get; set; }
	[Reactive] public string? NameOverride { get; set; }

	public HashSet<string> Files { get; set; }

	[Reactive] public ModioModData ModioData { get; set; }
	[Reactive] public NexusModsModData NexusModsData { get; set; }
	[Reactive] public GitHubModData GitHubData { get; set; }

	private static string GetDisplayName(string? name, string? fileName, string? folder, string uuid, bool isLooseMod, bool isToolkitProject, bool displayFileForName, string? nameOverride)
	{
		if (displayFileForName)
		{
			if (!isLooseMod)
			{
				if (fileName.IsValid()) return fileName;
			}
			else if(folder.IsValid())
			{
				if(isToolkitProject)
				{
					return folder + " [Toolkit Project]";
				}
				else
				{
					return folder + " [Loose Mod]";
				}
			}
		}
		else
		{
			if (nameOverride.IsValid()) return nameOverride;
			if (name.IsValid()) return name;
		}
		return "";
	}

	public virtual string GetHelpText()
	{
		return "";
	}

	private static string ExtenderStatusToToolTipText(ModExtenderStatus status, int requiredVersion, int currentVersion)
	{
		var result = "";

		if (requiredVersion > -1)
		{
			result += $"Requires Script Extender v{requiredVersion} or Higher";
		}
		else
		{
			result += "Requires the Script Extender";
		}

		if (status.HasFlag(ModExtenderStatus.DisabledFromConfig))
		{
			result += "\n(Enable Extensions in the Script Extender Settings)";
		}
		else if (status.HasFlag(ModExtenderStatus.MissingAppData))
		{
			result += $"\n(Missing %LOCALAPPDATA%\\..\\{DivinityApp.EXTENDER_APPDATA_DLL})";
		}
		else if (status.HasFlag(ModExtenderStatus.MissingUpdater))
		{
			result += $"\n(Missing {DivinityApp.EXTENDER_UPDATER_FILE})";
		}
		else if (status.HasFlag(ModExtenderStatus.MissingRequiredVersion))
		{
			result += "\n(The installed SE version is older)";
		}

		if (result != "")
		{
			result += Environment.NewLine;
		}

		if (currentVersion > -1)
		{
			if (status.HasFlag(ModExtenderStatus.MissingUpdater))
			{
				result += $"You are missing the Script Extender updater (DWrite.dll), which is required";
			}
			else
			{
				result += $"Currently installed version is v{currentVersion}";
			}
		}
		else
		{
			result += "No installed Script Extender version found\nIf you've already downloaded it, try opening the game once to complete the installation process";
		}
		return result;
	}

	private static ScriptExtenderIconType ExtenderModStatusToIcon(ModExtenderStatus status)
	{
		var result = ScriptExtenderIconType.None;

		if (status.HasFlag(ModExtenderStatus.DisabledFromConfig) || status.HasFlag(ModExtenderStatus.MissingUpdater))
		{
			result = ScriptExtenderIconType.Missing;
		}
		else if (status.HasFlag(ModExtenderStatus.MissingRequiredVersion) || status.HasFlag(ModExtenderStatus.MissingAppData))
		{
			result = ScriptExtenderIconType.Warning;
		}
		else if (status.HasFlag(ModExtenderStatus.Supports))
		{
			result = ScriptExtenderIconType.FulfilledSupports;
		}
		else if (status.HasFlag(ModExtenderStatus.Fulfilled))
		{
			result = ScriptExtenderIconType.FulfilledRequired;
		}

		return result;
	}

	public void AddTag(string tag)
	{
		if (tag.IsValid() && !Tags.Contains(tag))
		{
			Tags.Add(tag);
			Tags.Sort((x, y) => string.Compare(x, y, true));
		}
	}

	public void AddTags(IEnumerable<string> tags)
	{
		if (tags == null)
		{
			return;
		}
		var addedTags = false;
		foreach (var tag in tags)
		{
			if (tag.IsValid() && !Tags.Contains(tag))
			{
				Tags.Add(tag);
				addedTags = true;
			}
		}
		Tags.Sort((x, y) => string.Compare(x, y, true));
		if (addedTags)
		{
			this.RaisePropertyChanged("Tags");
		}
	}

	public bool PakEquals(string fileName, StringComparison comparison = StringComparison.Ordinal)
	{
		var outputPackage = Path.ChangeExtension(Folder, "pak");
		//Imported Classic Projects
		if (Folder.IsValid() && !Folder.Contains(UUID))
		{
			outputPackage = Path.ChangeExtension(Path.Join(Folder + "_" + UUID), "pak");
		}
		return outputPackage?.Equals(fileName, comparison) == true;
	}

	public bool IsNewerThan(DateTimeOffset date)
	{
		if (LastModified.HasValue)
		{
			return LastModified.Value > date;
		}
		return false;
	}

	public bool IsNewerThan(IModData mod)
	{
		if (LastModified.HasValue && mod.LastModified.HasValue)
		{
			return LastModified.Value > mod.LastModified.Value;
		}
		return false;
	}

	public string? GetURL(ModSourceType modSourceType, bool asProtocol = false)
	{
		switch (modSourceType)
		{
			case ModSourceType.MODIO:
				if (ModioData.IsEnabled)
				{
					return ModioData.ExternalLink;
				}
				break;
			case ModSourceType.NEXUSMODS:
				if (NexusModsData.IsEnabled)
				{
					return string.Format(DivinityApp.NEXUSMODS_MOD_URL, NexusModsData.ModId);
				}
				break;
			case ModSourceType.GITHUB:
				if (GitHubData.IsEnabled)
				{
					return $"https://github.com/{GitHubData.Author}/{GitHubData.Repository}";
				}
				break;
		}
		return "";
	}

	public List<string> GetAllURLs(bool asProtocol = false)
	{
		var urls = new List<string>();
		var modioUrl = GetURL(ModSourceType.MODIO, asProtocol);
		if (modioUrl.IsValid())
		{
			urls.Add(modioUrl);
		}
		var nexusUrl = GetURL(ModSourceType.NEXUSMODS, asProtocol);
		if (nexusUrl.IsValid())
		{
			urls.Add(nexusUrl);
		}
		var githubUrl = GetURL(ModSourceType.GITHUB, asProtocol);
		if (githubUrl.IsValid())
		{
			urls.Add(githubUrl);
		}
		return urls;
	}

	public override string ToString()
	{
		return $"Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID}) File({FilePath})";
	}

	public ModuleShortDesc ToModuleShortDesc()
	{
		return new ModuleShortDesc(UUID)
		{
			Folder = Folder,
			MD5 = MD5,
			Name = Name,
			Version = new LarianVersion(Version?.VersionInt ?? 0)
		};
	}

	public void AllowInLoadOrder(bool b)
	{
		ForceAllowInLoadOrder = b;
		IsActive = b && IsForceLoaded;
	}

	private static string OsirisStatusToTooltipText(DivinityOsirisModStatus status)
	{
		return status switch
		{
			DivinityOsirisModStatus.SCRIPTS => "Has Osiris Scripting",
			DivinityOsirisModStatus.MODFIXER => "Has Mod Fixer",
			_ => "",
		};
	}

	private static bool CanOpenModioBoolCheck(bool enabled, string? externalLink)
	{
		return enabled && externalLink.IsValid();
	}

	private static string NexusModsInfoToTooltip(DateTime createdDate, DateTime updatedDate, long endorsements)
	{
		var lines = new List<string>();

		if (endorsements > 0)
		{
			lines.Add($"Endorsements: {endorsements}");
		}

		if (createdDate != DateTime.MinValue)
		{
			lines.Add($"Created on {createdDate.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture)}");
		}

		if (updatedDate != DateTime.MinValue)
		{
			lines.Add($"Last updated on {createdDate.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture)}");
		}

		return string.Join("\n", lines);
	}

	private CompositeDisposable? _modConfigDisposables;
	private ModConfig? _modManagerConfig;

	public ModConfig? ModManagerConfig
	{
		get => _modManagerConfig;
		set
		{
			this.RaiseAndSetIfChanged(ref _modManagerConfig, value, nameof(ModManagerConfig));
			if (_modManagerConfig != null)
			{
				if (_modConfigDisposables == null)
				{
					_modConfigDisposables = [];

					this.WhenAnyValue(x => x.ModManagerConfig!.Notes).ToUIProperty(this, x => x.Notes, "").DisposeWith(_modConfigDisposables);

					this.WhenAnyValue(x => x.UUID).BindTo(ModManagerConfig, x => x.Id).DisposeWith(_modConfigDisposables);

					this.WhenAnyValue(x => x.NexusModsData.ModId).BindTo(this, x => x.ModManagerConfig!.NexusModsId).DisposeWith(_modConfigDisposables);
					this.WhenAnyValue(x => x.ModioData.NameId).BindTo(this, x => x.ModManagerConfig!.ModioId).DisposeWith(_modConfigDisposables);
					this.WhenAnyValue(x => x.GitHubData.Url).BindTo(this, x => x.ModManagerConfig!.GitHub).DisposeWith(_modConfigDisposables);
				}
			}
			else
			{
				_modConfigDisposables?.Dispose();
			}
		}
	}

	public void ApplyModConfig(ModConfig config)
	{
		if (ModManagerConfig != null)
		{
			if (config != ModManagerConfig) ModManagerConfig.SetFrom<ModConfig, ReactiveAttribute>(config);
		}
		else
		{
			ModManagerConfig = config;
		}

		if (config.NexusModsId > DivinityApp.NEXUSMODS_MOD_ID_START) NexusModsData.ModId = config.NexusModsId;
		if (config.ModioId.IsValid()) ModioData.NameId = config.ModioId;
		if (config.GitHub.IsValid()) GitHubData.Url = config.GitHub;
	}

	private static string GetAuthor(ValueTuple<string?, string?, string?, string?, bool> x)
	{
		var metaAuthor = x.Item1;
		var nexusAuthor = x.Item2;
		var githubAuthor = x.Item3;
		var modioAuthor = x.Item4;
		var isLarianMod = x.Item5;

		if (modioAuthor.IsValid()) return modioAuthor;
		if (metaAuthor.IsValid()) return metaAuthor;
		if (nexusAuthor.IsValid()) return nexusAuthor;
		if (githubAuthor.IsValid()) return githubAuthor;

		if (isLarianMod) return "Larian Studios";

		return string.Empty;
	}

	private static bool CanAddToLoadOrderCheck(ValueTuple<string?, bool, bool, bool, bool> x)
	{
		//x => x.ModType, x => x.IsHidden, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder
		var modType = x.Item1;
		var isHidden = x.Item2;
		var isForceLoaded = x.Item3;
		var isForceLoadedMergedMod = x.Item4;
		var forceAllowInLoadOrder = x.Item5;
		return modType != "Adventure" && !isHidden && (!isForceLoaded || isForceLoadedMergedMod || forceAllowInLoadOrder);
	}

	//Green
	private static readonly string EditorProjectBackgroundColor = "#0C00FF4D";
	private static readonly string EditorProjectBackgroundSelectedColor = "#3200ED48";
	private static readonly string EditorProjectBackgroundPointerOverColor = "#6400ED48";
	//Brownish
	private static readonly string ForceLoadedBackgroundColor = "#32C1AE00";
	private static readonly string ForceLoadedBackgroundSelectedColor = "#32F2DE00";
	private static readonly string ForceLoadedBackgroundPointerOverColor = "#64F2DE00";

	private void UpdateColors(ValueTuple<bool, bool, bool, bool, bool> x)
	{
		var isForceLoadedMergedMod = x.Item1;
		var isEditorMod = x.Item2;
		var isForceLoadedMod = x.Item3;
		var isActive = x.Item4 || x.Item5;

		if (isEditorMod)
		{
			SelectedColor = EditorProjectBackgroundSelectedColor;
			ListColor = EditorProjectBackgroundColor;
			PointerOverColor = EditorProjectBackgroundPointerOverColor;
		}
		else if (isForceLoadedMergedMod || isForceLoadedMod && isActive)
		{
			SelectedColor = ForceLoadedBackgroundSelectedColor;
			ListColor = ForceLoadedBackgroundColor;
			PointerOverColor = ForceLoadedBackgroundPointerOverColor;
		}
		else
		{
			ListColor = SelectedColor = PointerOverColor = string.Empty;
		}
	}

	private static bool CheckForInvalidUUID(ValueTuple<string?, bool> x)
	{
		var uuid = x.Item1;
		var canAddToLoadOrder = x.Item2;
		if (!canAddToLoadOrder) return false;
		if(uuid.IsValid())
		{
			return !Guid.TryParse(uuid, out _);
		}
		return false;
	}

	private string BuildMissingDependencyToolTip()
	{
		return $"Missing Dependencies:\n{string.Join(Environment.NewLine, MissingDependencies.Items.Select(x => x.Name).Order())}";
	}

	private static string ModToXml(string? pattern, ModData mod, bool isFixedString = false)
	{
		pattern ??= DivinityApp.XML_MODULE_SHORT_DESC;
		if (!isFixedString)
		{
			return string.Format(pattern ?? DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, System.Security.SecurityElement.Escape(mod.Name), mod.UUID, mod.Version.VersionInt, mod.PublishHandle);
		}
		else
		{
			return string.Format(pattern ?? DivinityApp.XML_MODULE_SHORT_DESC, mod.Folder, mod.MD5, System.Security.SecurityElement.Escape(mod.Name), mod.UUID, mod.Version.VersionInt);
		}
	}

	public string? Export(ModExportType exportType, string? pattern = null, bool isFixedString = false)
	{
		var result = exportType switch
		{
			ModExportType.XML => ModToXml(pattern, this, isFixedString),
			ModExportType.JSON => JsonSerializer.Serialize(this, JsonUtils.DefaultSerializerSettings),
			ModExportType.TXT => StringUtils.ModToTextLine(this),
			ModExportType.TSV => StringUtils.ModToTSVLine(this),
			_ => string.Empty,
		};
		return result;
	}

	private static string GetModDisplayTypeName(ValueTuple<bool, bool, bool, string?> x)
	{
		(bool isLooseMod, bool IsToolkitProject, bool isForceLoaded, string? modType) = x;

		if (IsToolkitProject) return "Toolkit Project";
		if (isLooseMod) return "Loose Mod";
		if (isForceLoaded) return "Override";
		return modType != "Adventure" ? "Add-on" : "Adventure";
	}

	private static string GetModDisplayTypeForeground(ValueTuple<bool, bool, bool, string?> x)
	{
		(bool isLooseMod, bool IsToolkitProject, bool isForceLoaded, string? modType) = x;

		if (IsToolkitProject) return "Lime";
		if (isLooseMod) return "Yellow";
		if (isForceLoaded) return "Orange";
		return string.Empty;
	}

	public ModData(string uuid)
	{
		Version = LarianVersion.Empty;
		HeaderVersion = LarianVersion.Empty;
		PublishVersion = LarianVersion.Empty;
		MD5 = "";
		Author = "";
		Folder = "";
		UUID = uuid;
		Name = "";
		PublishHandle = 0ul;
		FileSize = 0ul;

		HelpText = "";
		Index = -1;
		CanDrag = true;
		IsVisible = true;

		Dependencies = new SourceCache<ModuleShortDesc, string>(x => x.UUID);
		MissingDependencies = new SourceCache<ModuleShortDesc, string>(x => x.UUID);
		Conflicts = new SourceCache<ModuleShortDesc, string>(x => x.UUID);

		Tags = [];
		Files = [];

		ModioData = new ModioModData();
		NexusModsData = new NexusModsModData();
		GitHubData = new GitHubModData();

		this.WhenAnyValue(x => x.FilePath).Select(f => Path.GetFileName(f)).BindTo(this, x => x.FileName);
		this.WhenAnyValue(x => x.Author, x => x.NexusModsData.Author, x => x.GitHubData.Author, x => x.ModioData.Author, x => x.IsLarianMod).Select(GetAuthor).BindTo(this, x => x.AuthorDisplayName);

		this.WhenAnyValue(x => x.Name, x => x.FileName, x => x.Folder, x => x.UUID, x => x.IsLooseMod, 
			x => x.IsToolkitProject, x => x.DisplayFileForName, x => x.NameOverride, GetDisplayName)
			.ObserveOn(RxApp.MainThreadScheduler)
			.BindTo(this, x => x.DisplayName);

		this.WhenAnyValue(x => x.Description).Select(Validators.IsValid).ToUIProperty(this, x => x.HasDescription);

		this.WhenAnyValue(x => x.AuthorDisplayName).Select(Validators.IsValid).ToUIPropertyImmediate(this, x => x.HasAuthor);

		this.WhenAnyValue(x => x.ModType, x => x.IsHidden, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder)
			.Select(CanAddToLoadOrderCheck).ToUIPropertyImmediate(this, x => x.CanAddToLoadOrder, true);

		this.WhenAnyValue(x => x.UUID).BindTo(NexusModsData, x => x.UUID);
		this.WhenAnyValue(x => x.NexusModsData.PictureUrl).Select(Validators.IsValid).ToUIProperty(this, x => x.NexusImageVisibility);
		this.WhenAnyValue(x => x.NexusModsData.IsUpdated).ToUIProperty(this, x => x.NexusModsInformationVisibility);

		this.WhenAnyValue(x => x.NexusModsData.CreatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToUIProperty(this, x => x.NexusModsCreatedDate);
		this.WhenAnyValue(x => x.NexusModsData.UpdatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToUIProperty(this, x => x.NexusModsUpdatedDate);

		this.WhenAnyValue(x => x.NexusModsCreatedDate, x => x.NexusModsUpdatedDate, x => x.NexusModsData.EndorsementCount)
			.Select(x => NexusModsInfoToTooltip(x.Item1, x.Item2, x.Item3)).ToUIProperty(this, x => x.NexusModsTooltipInfo);

		this.WhenAnyValue(x => x.IsForceLoaded, x => x.HasMetadata, x => x.IsForceLoadedMergedMod)
			.Select(b => b.Item1 && b.Item2 && !b.Item3)
			.ToUIProperty(this, x => x.CanForceAllowInLoadOrder);

		this.WhenAnyValue(x => x.GitHubEnabled, x => x.GitHubData.IsEnabled, (b1, b2) => b1 && b2)
			.ToUIProperty(this, x => x.HasGitHubLink);

		this.WhenAnyValue(x => x.NexusModsEnabled, x => x.NexusModsData.ModId, (b, id) => b && id >= DivinityApp.NEXUSMODS_MOD_ID_START)
			.ToUIProperty(this, x => x.HasNexusModsLink);

		this.WhenAnyValue(x => x.ModioEnabled, x => x.ModioData.ExternalLink, CanOpenModioBoolCheck)
			.ToUIProperty(this, x => x.HasModioLink);

		this.WhenAnyValue(x => x.HasGitHubLink, x => x.HasNexusModsLink, x => x.HasModioLink)
			.Select(x => x.Item1 || x.Item2 || x.Item3)
			.ToUIProperty(this, x => x.HasAnyExternalLink);

		var dependenciesChanged = Dependencies.CountChanged;
		dependenciesChanged.ToUIProperty(this, x => x.TotalDependencies);
		dependenciesChanged.Select(x => x > 0).ToUIPropertyImmediate(this, x => x.HasDependencies);
		Dependencies.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(out _displayedDependencies, Sorters.ModuleShortDesc)
			.Subscribe();

		var conflictsChanged = Conflicts.CountChanged;
		conflictsChanged.ToUIProperty(this, x => x.TotalConflicts);
		conflictsChanged.Select(x => x > 0).ToUIPropertyImmediate(this, x => x.HasConflicts);
		Conflicts.Connect()
			.ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(out _displayedConflicts, Sorters.ModuleShortDesc)
			.Subscribe();

		var missingDepConn = MissingDependencies.Connect().ObserveOn(RxApp.MainThreadScheduler);
		missingDepConn.Count().Select(x => x > 0).ToUIPropertyImmediate(this, x => x.HasMissingDependency);
		missingDepConn.Select(x => BuildMissingDependencyToolTip()).ToUIProperty(this, x => x.MissingDependencyToolTip, string.Empty);

		this.WhenAnyValue(x => x.UUID, x => x.CanAddToLoadOrder)
			.Select(CheckForInvalidUUID)
			.ToUIPropertyImmediate(this, x => x.HasInvalidUUID);

		this.WhenAnyValue(x => x.IsLooseMod, x => x.DisplayExtraIcons)
			.AllTrue()
			.ToUIPropertyImmediate(this, x => x.HasToolkitIcon);

		this.WhenAnyValue(x => x.IsActive, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder).Subscribe((b) =>
		{
			var isActive = b.Item1;
			var isForceLoaded = b.Item2;
			var isForceLoadedMergedMod = b.Item3;
			var forceAllowInLoadOrder = b.Item4;

			if (forceAllowInLoadOrder || isActive)
			{
				CanDrag = true;
			}
			else
			{
				CanDrag = !isForceLoaded || isForceLoadedMergedMod;
			}
		});

		this.WhenAnyValue(x => x.ListColor, x => x.SelectedColor)
			.Select(x => x.Item1.IsValid() || x.Item2.IsValid())
			.BindTo(this, x => x.HasColorOverride);

		this.WhenAnyValue(x => x.IsForceLoadedMergedMod, x => x.IsLooseMod, x => x.IsForceLoaded, x => x.ForceAllowInLoadOrder, x => x.IsActive)
			.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateColors);

		this.WhenAnyValue(x => x.Description, x => x.HasDependencies, x => x.UUID).
			Select(x => x.Item1.IsValid() || x.Item2 || x.Item3.IsValid())
			.ToUIProperty(this, x => x.HasToolTip, true);

		var whenModDisplayType = this.WhenAnyValue(x => x.IsLooseMod, x => x.IsToolkitProject, x => x.IsForceLoaded, x => x.ModType);
		whenModDisplayType.Select(GetModDisplayTypeName)
			.ToUIProperty(this, x => x.ModDisplayTypeName, "Add-on");

		whenModDisplayType.Select(GetModDisplayTypeForeground)
			.ToUIProperty(this, x => x.ModDisplayTypeForeground, string.Empty);

		this.WhenAnyValue(x => x.IsLooseMod, x => x.IsHidden, x => x.FilePath,
			(isEditorMod, isHidden, path) => !isEditorMod && !isHidden && path.IsValid())
			.ToUIPropertyImmediate(this, x => x.CanDelete);

		var whenExtenderProp = this.WhenAnyValue(x => x.ExtenderModStatus, x => x.CurrentExtenderVersion, 
			x => x.ScriptExtenderData, x => x.ScriptExtenderData!.RequiredVersion, 
			(status, curVersion, seData, _) => ExtenderStatusToToolTipText(status, seData?.RequiredVersion ?? 0, curVersion))
			.ToUIProperty(this, x => x.ScriptExtenderSupportToolTipText);

		this.WhenAnyValue(x => x.ExtenderModStatus).Select(x => x != ModExtenderStatus.None)
			.ToUIPropertyImmediate(this, x => x.HasExtenderStatus);

		var whenOsirisStatusChanges = this.WhenAnyValue(x => x.OsirisModStatus);
		whenOsirisStatusChanges.Select(x => x != DivinityOsirisModStatus.NONE).ToUIProperty(this, x => x.HasOsirisStatus);
		whenOsirisStatusChanges.Select(OsirisStatusToTooltipText).ToUIProperty(this, x => x.OsirisStatusToolTipText);

		this.WhenAnyValue(x => x.ExtenderModStatus).Select(ExtenderModStatusToIcon).ToUIPropertyImmediate(this, x => x.ExtenderIcon);

		this.WhenAnyValue(x => x.Notes).Select(Validators.IsValid).ToUIProperty(this, x => x.HasNotes);

		this.WhenAnyValue(x => x.LastModified).SkipWhile(x => !x.HasValue)
			.Select(x => x!.Value.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture))
			.ToUIProperty(this, x => x.LastModifiedDateText, "");

		this.WhenAnyValue(x => x.FilePath).Select(Validators.IsValid).ToUIProperty(this, x => x.HasFilePath);
		this.WhenAnyValue(x => x.Version.Version).ToUIProperty(this, x => x.DisplayVersion, "0.0.0.0");

		this.WhenAnyValue(x => x.DisplayFileForName).Select(x => x ? "Show Mod Display Name" : "Show File Name").ToUIProperty(this, x => x.ToggleModNameLabel);
		this.WhenAnyValue(x => x.ForceAllowInLoadOrder).Select(x => x ? "Remove from Load Order" : "Allow in Load Order").ToUIProperty(this, x => x.ForceAllowInLoadOrderLabel);

		this.WhenAnyValue(x => x.ModioData.Description).Subscribe(desc =>
		{
			if(desc.IsValid() && desc.Length > Description?.Length)
			{
				Description = desc;
			}
		});

		this.WhenAnyValue(x => x.ModioData.LastUpdated).Subscribe(lastUpdated =>
		{
			if(lastUpdated != DateTimeOffset.MinValue)
			{
				LastModified = lastUpdated;
			}
		});

		SetIsBaseGameMod(false);
	}

	public void SetIsBaseGameMod(bool isBaseGameMod)
	{
		if (!isBaseGameMod)
		{
			IsHidden = false;
			if (ModManagerConfig == null)
			{
				ModManagerConfig = new ModConfig
				{
					Id = UUID
				};
			}
		}
		else
		{
			IsHidden = true;
			ModManagerConfig = null;
		}
	}

	public static ModData Clone(ModData mod)
	{
		var cloneMod = new ModData(mod.UUID)
		{
			HasMetadata = mod.HasMetadata,
			Name = mod.Name,
			Author = mod.Author,
			Version = new LarianVersion(mod.Version.VersionInt),
			HeaderVersion = new LarianVersion(mod.HeaderVersion.VersionInt),
			PublishVersion = new LarianVersion(mod.PublishVersion.VersionInt),
			Folder = mod.Folder,
			Description = mod.Description,
			MD5 = mod.MD5,
			ModType = mod.ModType,
			Tags = [.. mod.Tags]
		};
		cloneMod.Conflicts.AddOrUpdate(mod.Conflicts.Items);
		cloneMod.Dependencies.AddOrUpdate(mod.Dependencies.Items);
		cloneMod.NexusModsData.Update(mod.NexusModsData);
		cloneMod.ModioData.Update(mod.ModioData.Data);
		cloneMod.GitHubData.Update(mod.GitHubData);
		if (mod.ModManagerConfig != null) cloneMod.ApplyModConfig(mod.ModManagerConfig);
		return cloneMod;
	}
}
