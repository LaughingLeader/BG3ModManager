using DynamicData;
using DynamicData.Binding;

using ModManager.Extensions;
using ModManager.Models.GitHub;
using ModManager.Models.NexusMods;
using ModManager.Models.Steam;
using ModManager.Util;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows;

namespace ModManager.Models.Mod;

[DataContract]
[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
public class DivinityModData : ReactiveObject, IDivinityModData, IModEntry, ISelectable
{
	public ModEntryType EntryType => ModEntryType.Mod;

	[Reactive] public string FilePath { get; set; }

	#region meta.lsx Properties
	[Reactive, DataMember] public string UUID { get; set; }
	[Reactive, DataMember] public string Folder { get; set; }
	[Reactive, DataMember] public string Name { get; set; }
	[Reactive, DataMember] public string Description { get; set; }
	[Reactive, DataMember] public string Author { get; set; }
	[Reactive, DataMember] public string ModType { get; set; }
	[Reactive] public string MD5 { get; set; }
	[Reactive, DataMember] public LarianVersion Version { get; set; }
	[Reactive] public LarianVersion HeaderVersion { get; set; }
	[Reactive] public LarianVersion PublishVersion { get; set; }
	public List<string> Tags { get; set; } = [];

	public ObservableCollectionExtended<string> Modes { get; private set; } = [];

	public string Targets { get; set; }
	public SourceList<DivinityModDependencyData> Dependencies { get; set; } = new SourceList<DivinityModDependencyData>();
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
	[Reactive] public string BuiltinOverrideModsText { get; set; }

	[Reactive] public string HelpText { get; set; }

	[Reactive] public Visibility Visibility { get; set; }

	[Reactive] public int Index { get; set; }

	public string OutputPakName
	{
		get
		{
			if (!Folder.Contains(UUID))
			{
				return Path.ChangeExtension($"{Folder}_{UUID}", "pak");
			}
			else
			{
				return Path.ChangeExtension($"{FileName}", "pak");
			}
		}
	}

	[Reactive] public DateTimeOffset? LastUpdated { get; set; }

	[Reactive] public DivinityExtenderModStatus ExtenderModStatus { get; set; }
	[Reactive] public DivinityOsirisModStatus OsirisModStatus { get; set; }

	[Reactive] public int CurrentExtenderVersion { get; set; }

	[Reactive] public DivinityModScriptExtenderConfig ScriptExtenderData { get; set; }


	protected ReadOnlyObservableCollection<DivinityModDependencyData> _displayedDependencies;
	public ReadOnlyObservableCollection<DivinityModDependencyData> DisplayedDependencies => _displayedDependencies;

	[ObservableAsProperty] public bool HasToolTip { get; }
	[ObservableAsProperty] public int TotalDependencies { get; }
	[ObservableAsProperty] public bool HasDependencies { get; }

	[Reactive] public bool HasScriptExtenderSettings { get; set; }

	[Reactive] public bool IsEditorMod { get; set; }

	[Reactive] public bool IsActive { get; set; }

	private bool isSelected = false;

	public bool IsSelected
	{
		get => isSelected;
		set
		{
			if (value && Visibility != Visibility.Visible)
			{
				value = false;
			}
			this.RaiseAndSetIfChanged(ref isSelected, value);
		}
	}

	//These properties may be accessed from code, so they need BindTo in order to be updated as soon as possible.
	[Reactive] public string FileName { get; private set; }
	[Reactive] public string AuthorDisplayName { get; private set; }

	// This is a property instead of an ObservableAsProperty so the name is set immediately
	[Reactive] public string DisplayName { get; private set; }
	[ObservableAsProperty] public bool CanAddToLoadOrder { get; }
	[ObservableAsProperty] public bool CanDelete { get; }
	[ObservableAsProperty] public string ScriptExtenderSupportToolTipText { get; }
	[ObservableAsProperty] public string OsirisStatusToolTipText { get; }
	[ObservableAsProperty] public string LastModifiedDateText { get; }
	[ObservableAsProperty] public string DisplayVersion { get; }
	[ObservableAsProperty] public string Notes { get; }
	[ObservableAsProperty] public Visibility DescriptionVisibility { get; }
	[ObservableAsProperty] public Visibility AuthorVisibility { get; }
	[ObservableAsProperty] public Visibility DependencyVisibility { get; }
	[ObservableAsProperty] public Visibility OpenGitHubLinkVisibility { get; }
	[ObservableAsProperty] public Visibility OpenNexusModsLinkVisibility { get; }
	[ObservableAsProperty] public Visibility OpenWorkshopLinkVisibility { get; }
	[ObservableAsProperty] public Visibility ToggleForceAllowInLoadOrderVisibility { get; }
	[ObservableAsProperty] public Visibility ExtenderStatusVisibility { get; }
	[ObservableAsProperty] public Visibility OsirisStatusVisibility { get; }
	[ObservableAsProperty] public Visibility NotesVisibility { get; }
	[ObservableAsProperty] public Visibility HasFilePathVisibility { get; }

	#region NexusMods Properties
	[ObservableAsProperty] public Visibility NexusImageVisibility { get; }
	[ObservableAsProperty] public Visibility NexusModsInformationVisibility { get; }
	[ObservableAsProperty] public DateTime NexusModsCreatedDate { get; }
	[ObservableAsProperty] public DateTime NexusModsUpdatedDate { get; }
	[ObservableAsProperty] public string NexusModsTooltipInfo { get; }

	#endregion

	[Reactive] public bool GitHubEnabled { get; set; }
	[Reactive] public bool NexusModsEnabled { get; set; }
	[Reactive] public bool SteamWorkshopEnabled { get; set; }
	[Reactive] public bool CanDrag { get; set; }
	[Reactive] public bool DeveloperMode { get; set; }
	[Reactive] public string SelectedColor { get; set; }
	[Reactive] public string ListColor { get; set; }
	[Reactive] public bool HasColorOverride { get; set; }

	public HashSet<string> Files { get; set; }

	[Reactive] public SteamWorkshopModData WorkshopData { get; set; }
	[Reactive] public NexusModsModData NexusModsData { get; set; }
	[Reactive] public GitHubModData GitHubData { get; set; }

	private static string GetDisplayName(ValueTuple<string, string, string, string, bool, bool> x)
	{
		var name = x.Item1;
		var fileName = x.Item2;
		var folder = x.Item3;
		var uuid = x.Item4;
		var isEditorMod = x.Item5;
		var displayFileForName = x.Item6;
		if (displayFileForName)
		{
			if (!isEditorMod)
			{
				return fileName;
			}
			else
			{
				return folder + " [Editor Project]";
			}
		}
		else
		{
			if (!DivinityApp.DeveloperModeEnabled && uuid == DivinityApp.MAIN_CAMPAIGN_UUID)
			{
				return "Main";
			}
			return name;
		}
	}

	public virtual string GetHelpText()
	{
		return "";
	}

	private static string ExtenderStatusToToolTipText(DivinityExtenderModStatus status, int requiredVersion, int currentVersion)
	{
		var result = "";
		switch (status)
		{
			case DivinityExtenderModStatus.REQUIRED:
			case DivinityExtenderModStatus.REQUIRED_MISSING:
			case DivinityExtenderModStatus.REQUIRED_DISABLED:
			case DivinityExtenderModStatus.REQUIRED_OLD:
			case DivinityExtenderModStatus.REQUIRED_MISSING_UPDATER:
				if (status == DivinityExtenderModStatus.REQUIRED_MISSING)
				{
					result = "[MISSING] ";
				}
				else if (status == DivinityExtenderModStatus.REQUIRED_MISSING_UPDATER)
				{
					result = "[SE DISABLED] ";
				}
				else if (status == DivinityExtenderModStatus.REQUIRED_DISABLED)
				{
					result = "[EXTENDER DISABLED] ";
				}
				else if (status == DivinityExtenderModStatus.REQUIRED_OLD)
				{
					result = "[OLD] ";
				}
				if (requiredVersion > -1)
				{
					result += $"Requires Script Extender v{requiredVersion} or Higher";
				}
				else
				{
					result += "Requires the Script Extender";
				}
				if (status == DivinityExtenderModStatus.REQUIRED_DISABLED)
				{
					result += "\n(Enable Extensions in the Script Extender Settings)";
				}
				else if (status == DivinityExtenderModStatus.REQUIRED_MISSING_UPDATER)
				{
					result += "\n(Missing DWrite.dll)";
				}
				else if (status == DivinityExtenderModStatus.REQUIRED_OLD)
				{
					result += "\n(The installed SE version is older)";
				}
				break;
			case DivinityExtenderModStatus.SUPPORTS:
				if (requiredVersion > -1)
				{
					result = $"Uses Script Extender v{requiredVersion} or Higher (Optional)";
				}
				else
				{
					result = "Uses the Script Extender (Optional)";
				}
				break;
			case DivinityExtenderModStatus.NONE:
			default:
				result = "";
				break;
		}
		if (result != "")
		{
			result += Environment.NewLine;
		}
		if (currentVersion > -1)
		{
			result += $"Currently installed version is v{currentVersion}";
		}
		else
		{
			result += "No installed extender version found";
		}
		return result;
	}

	public void AddTag(string tag)
	{
		if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
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
			if (!string.IsNullOrWhiteSpace(tag) && !Tags.Contains(tag))
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
		if (!Folder.Contains(UUID))
		{
			outputPackage = Path.ChangeExtension(Path.Join(Folder + "_" + UUID), "pak");
		}
		return outputPackage.Equals(fileName, comparison);
	}

	public bool IsNewerThan(DateTimeOffset date)
	{
		if (LastModified.HasValue)
		{
			return LastModified.Value > date;
		}
		return false;
	}

	public bool IsNewerThan(IDivinityModData mod)
	{
		if (LastModified.HasValue && mod.LastModified.HasValue)
		{
			return LastModified.Value > mod.LastModified.Value;
		}
		return false;
	}

	public string GetURL(ModSourceType modSourceType, bool asProtocol = false)
	{
		switch (modSourceType)
		{
			case ModSourceType.STEAM:
				if (WorkshopData.IsEnabled)
				{
					if (!asProtocol)
					{
						return $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopData.ModId}";
					}
					else
					{
						return $"steam://url/CommunityFilePage/{WorkshopData.ModId}";
					}
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
		var steamUrl = GetURL(ModSourceType.STEAM, asProtocol);
		if (!string.IsNullOrEmpty(steamUrl))
		{
			urls.Add(steamUrl);
		}
		var nexusUrl = GetURL(ModSourceType.NEXUSMODS, asProtocol);
		if (!string.IsNullOrEmpty(nexusUrl))
		{
			urls.Add(nexusUrl);
		}
		var githubUrl = GetURL(ModSourceType.GITHUB, asProtocol);
		if (!string.IsNullOrEmpty(githubUrl))
		{
			urls.Add(githubUrl);
		}
		return urls;
	}

	public override string ToString()
	{
		return $"Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID}) File({FilePath})";
	}

	public DivinityLoadOrderEntry ToOrderEntry()
	{
		return new DivinityLoadOrderEntry
		{
			UUID = UUID,
			Name = Name
		};
	}

	public DivinityProfileActiveModData ToProfileModData()
	{
		return new DivinityProfileActiveModData()
		{
			Folder = Folder,
			MD5 = MD5,
			Name = Name,
			UUID = UUID,
			Version = Version.VersionInt
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

	private static bool CanOpenWorkshopBoolCheck(bool enabled, bool isHidden, bool isLarianMod, long workshopID)
	{
		return enabled && !isHidden & !isLarianMod & workshopID > DivinityApp.WORKSHOP_MOD_ID_START;
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


	private CompositeDisposable _modConfigDisposables;
	private ModConfig _modManagerConfig;

	public ModConfig ModManagerConfig
	{
		get => _modManagerConfig;
		set
		{
			this.RaiseAndSetIfChanged(ref _modManagerConfig, value);
			if (_modManagerConfig != null)
			{
				if (_modConfigDisposables == null)
				{
					_modConfigDisposables = [];

					this.WhenAnyValue(x => x.ModManagerConfig.Notes).ToUIProperty(this, x => x.Notes, "").DisposeWith(_modConfigDisposables);

					this.WhenAnyValue(x => x.UUID).BindTo(ModManagerConfig, x => x.Id).DisposeWith(_modConfigDisposables);

					this.WhenAnyValue(x => x.NexusModsData.ModId).BindTo(this, x => x.ModManagerConfig.NexusModsId).DisposeWith(_modConfigDisposables);
					this.WhenAnyValue(x => x.WorkshopData.ModId).BindTo(this, x => x.ModManagerConfig.SteamWorkshopId).DisposeWith(_modConfigDisposables);
					this.WhenAnyValue(x => x.GitHubData.Url).BindTo(this, x => x.ModManagerConfig.GitHub).DisposeWith(_modConfigDisposables);
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
		if (config.SteamWorkshopId > DivinityApp.WORKSHOP_MOD_ID_START) WorkshopData.ModId = config.SteamWorkshopId;
		if (!string.IsNullOrWhiteSpace(config.GitHub)) GitHubData.Url = config.GitHub;
	}

	private static string GetAuthor(ValueTuple<string, string, string, bool> x)
	{
		var metaAuthor = x.Item1;
		var nexusAuthor = x.Item2;
		var githubAuthor = x.Item3;
		var isLarianMod = x.Item4;

		if (!string.IsNullOrEmpty(metaAuthor)) return metaAuthor;
		if (!string.IsNullOrEmpty(nexusAuthor)) return nexusAuthor;
		if (!string.IsNullOrEmpty(githubAuthor)) return githubAuthor;

		if (isLarianMod) return "Larian Studios";

		return string.Empty;
	}

	private static bool CanAddToLoadOrderCheck(ValueTuple<string, bool, bool, bool, bool> x)
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
	private static readonly string EditorProjectBackgroundSelectedColor = "#6400ED48";
	//Brownish
	private static readonly string ForceLoadedBackgroundColor = "#32C17200";
	private static readonly string ForceLoadedBackgroundSelectedColor = "#64F38F00";

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
		}
		else if (isForceLoadedMergedMod || isForceLoadedMod && isActive)
		{
			SelectedColor = ForceLoadedBackgroundSelectedColor;
			ListColor = ForceLoadedBackgroundColor;
		}
		else
		{
			ListColor = SelectedColor = string.Empty;
		}
	}

	private static readonly JsonSerializerSettings _serializerSettings = new()
	{
		NullValueHandling = NullValueHandling.Ignore,
		Formatting = Formatting.Indented,
	};

	public string Export(ModExportType exportType)
	{
		var result = exportType switch
		{
			ModExportType.XML => String.Format(DivinityApp.XML_MODULE_SHORT_DESC, Folder, MD5, System.Security.SecurityElement.Escape(Name), UUID, Version.VersionInt),
			ModExportType.JSON => JsonConvert.SerializeObject(this, _serializerSettings),
			ModExportType.TXT => StringUtils.ModToTextLine(this),
			ModExportType.TSV => StringUtils.ModToTSVLine(this),
			_ => String.Empty,
		};
		return result;
	}

	public DivinityModData()
	{
		Version = LarianVersion.Empty;
		HeaderVersion = LarianVersion.Empty;
		PublishVersion = LarianVersion.Empty;
		MD5 = "";
		Author = "";
		Folder = "";
		UUID = "";
		Name = "";

		HelpText = "";
		Targets = "";
		Index = -1;
		CanDrag = true;

		WorkshopData = new SteamWorkshopModData();
		NexusModsData = new NexusModsModData();
		GitHubData = new GitHubModData();

		this.WhenAnyValue(x => x.FilePath).Select(f => Path.GetFileName(f)).BindTo(this, x => x.FileName);
		this.WhenAnyValue(x => x.Author, x => x.NexusModsData.Author, x => x.GitHubData.Author, x => x.IsLarianMod).Select(GetAuthor).BindTo(this, x => x.AuthorDisplayName);

		this.WhenAnyValue(x => x.Name, x => x.FilePath, x => x.Folder, x => x.UUID, x => x.IsEditorMod, x => x.DisplayFileForName)
			.Select(GetDisplayName).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.DisplayName);
		this.WhenAnyValue(x => x.Description).Select(PropertyConverters.StringToVisibility).ToUIProperty(this, x => x.DescriptionVisibility);

		this.WhenAnyValue(x => x.AuthorDisplayName).Select(PropertyConverters.StringToVisibility).ToUIPropertyImmediate(this, x => x.AuthorVisibility);

		this.WhenAnyValue(x => x.ModType, x => x.IsHidden, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder)
			.Select(CanAddToLoadOrderCheck).ToUIPropertyImmediate(this, x => x.CanAddToLoadOrder, true);

		this.WhenAnyValue(x => x.UUID).BindTo(NexusModsData, x => x.UUID);
		this.WhenAnyValue(x => x.NexusModsData.PictureUrl)
			.Select(uri => uri != null && !string.IsNullOrEmpty(uri.AbsolutePath) ? Visibility.Visible : Visibility.Collapsed)
			.ToUIProperty(this, x => x.NexusImageVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.NexusModsData.IsUpdated)
			.Select(PropertyConverters.BoolToVisibility)
			.ToUIProperty(this, x => x.NexusModsInformationVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.NexusModsData.CreatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToUIProperty(this, x => x.NexusModsCreatedDate);
		this.WhenAnyValue(x => x.NexusModsData.UpdatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToUIProperty(this, x => x.NexusModsUpdatedDate);

		this.WhenAnyValue(x => x.NexusModsCreatedDate, x => x.NexusModsUpdatedDate, x => x.NexusModsData.EndorsementCount)
			.Select(x => NexusModsInfoToTooltip(x.Item1, x.Item2, x.Item3)).ToUIProperty(this, x => x.NexusModsTooltipInfo);

		this.WhenAnyValue(x => x.IsForceLoaded, x => x.HasMetadata, x => x.IsForceLoadedMergedMod)
			.Select(b => b.Item1 && b.Item2 && !b.Item3 ? Visibility.Visible : Visibility.Collapsed)
			.ToUIProperty(this, x => x.ToggleForceAllowInLoadOrderVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.GitHubEnabled, x => x.GitHubData.IsEnabled, (b1, b2) => b1 && b2)
			.Select(PropertyConverters.BoolToVisibility)
			.ToUIProperty(this, x => x.OpenGitHubLinkVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.NexusModsEnabled, x => x.NexusModsData.ModId, (b, id) => b && id >= DivinityApp.NEXUSMODS_MOD_ID_START)
			.Select(PropertyConverters.BoolToVisibility)
			.ToUIProperty(this, x => x.OpenNexusModsLinkVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.SteamWorkshopEnabled, x => x.IsHidden, x => x.IsLarianMod, x => x.WorkshopData.ModId, CanOpenWorkshopBoolCheck)
			.Select(PropertyConverters.BoolToVisibility)
			.ToUIProperty(this, x => x.OpenWorkshopLinkVisibility, Visibility.Collapsed);

		var dependenciesChanged = Dependencies.CountChanged;
		dependenciesChanged.ToUIProperty(this, x => x.TotalDependencies);
		dependenciesChanged.Select(x => x > 0).ToUIProperty(this, x => x.HasDependencies);
		dependenciesChanged.Select(x => PropertyConverters.BoolToVisibility(x > 0)).ToUIProperty(this, x => x.DependencyVisibility, Visibility.Collapsed);
		Dependencies.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out _displayedDependencies).Subscribe();

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
			.Select(x => !string.IsNullOrEmpty(x.Item1) || !string.IsNullOrEmpty(x.Item2))
			.BindTo(this, x => x.HasColorOverride);
		this.WhenAnyValue(x => x.IsForceLoadedMergedMod, x => x.IsEditorMod, x => x.IsForceLoaded, x => x.ForceAllowInLoadOrder, x => x.IsActive)
			.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateColors);

		// If a screen reader is active, don't bother making tooltips for the mod item entry
		this.WhenAnyValue(x => x.Description, x => x.HasDependencies, x => x.UUID).
			Select(x => !DivinityApp.IsScreenReaderActive() && (!string.IsNullOrEmpty(x.Item1) || x.Item2 || !string.IsNullOrEmpty(x.Item3)))
			.ToUIProperty(this, x => x.HasToolTip, true);

		this.WhenAnyValue(x => x.IsEditorMod, x => x.IsHidden, x => x.FilePath,
			(isEditorMod, isHidden, path) => !isEditorMod && !isHidden && !string.IsNullOrEmpty(path)).ToUIPropertyImmediate(this, x => x.CanDelete);

		var whenExtenderProp = this.WhenAnyValue(x => x.ExtenderModStatus, x => x.ScriptExtenderData.RequiredVersion, x => x.CurrentExtenderVersion);
		whenExtenderProp.Select(x => ExtenderStatusToToolTipText(x.Item1, x.Item2, x.Item3)).ToUIProperty(this, x => x.ScriptExtenderSupportToolTipText);
		this.WhenAnyValue(x => x.ExtenderModStatus).Select(x => x != DivinityExtenderModStatus.NONE ? Visibility.Visible : Visibility.Collapsed)
			.ToUIPropertyImmediate(this, x => x.ExtenderStatusVisibility, Visibility.Collapsed);

		var whenOsirisStatusChanges = this.WhenAnyValue(x => x.OsirisModStatus);
		whenOsirisStatusChanges.Select(x => x != DivinityOsirisModStatus.NONE ? Visibility.Visible : Visibility.Collapsed).ToUIProperty(this, x => x.OsirisStatusVisibility);
		whenOsirisStatusChanges.Select(OsirisStatusToTooltipText).ToUIProperty(this, x => x.OsirisStatusToolTipText);

		this.WhenAnyValue(x => x.Notes).Select(PropertyConverters.StringToVisibility).ToUIProperty(this, x => x.NotesVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.LastUpdated).SkipWhile(x => !x.HasValue)
			.Select(x => $"Last Modified on {x.Value.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture)}")
			.ToUIProperty(this, x => x.LastModifiedDateText, "");

		this.WhenAnyValue(x => x.FilePath).Select(PropertyConverters.StringToVisibility).ToUIProperty(this, x => x.HasFilePathVisibility, Visibility.Collapsed);
		this.WhenAnyValue(x => x.Version.Version).ToUIProperty(this, x => x.DisplayVersion, "0.0.0.0");

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

	public static DivinityModData Clone(DivinityModData mod)
	{
		var cloneMod = new DivinityModData()
		{
			HasMetadata = mod.HasMetadata,
			UUID = mod.UUID,
			Name = mod.Name,
			Author = mod.Author,
			Version = new LarianVersion(mod.Version.VersionInt),
			HeaderVersion = new LarianVersion(mod.HeaderVersion.VersionInt),
			PublishVersion = new LarianVersion(mod.PublishVersion.VersionInt),
			Folder = mod.Folder,
			Description = mod.Description,
			MD5 = mod.MD5,
			ModType = mod.ModType,
			Tags = mod.Tags.ToList(),
			Targets = mod.Targets,
		};
		cloneMod.Dependencies.AddRange(mod.Dependencies.Items);
		cloneMod.NexusModsData.Update(mod.NexusModsData);
		cloneMod.WorkshopData.Update(mod.WorkshopData);
		cloneMod.GitHubData.Update(mod.GitHubData);
		cloneMod.ApplyModConfig(mod.ModManagerConfig);
		return cloneMod;
	}
}
