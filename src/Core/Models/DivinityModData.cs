

using DivinityModManager.Models.Github;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.Util;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using System.Globalization;
using System.Windows;

namespace DivinityModManager.Models;

[ScreenReaderHelper(Name = "DisplayName", HelpText = "HelpText")]
public class DivinityModData : DivinityBaseModData, ISelectable
{
	private static readonly SortExpressionComparer<ModuleShortDesc> _moduleSort = SortExpressionComparer<ModuleShortDesc>
		.Ascending(p => !DivinityApp.IgnoredMods.Any(x => x.UUID == p.UUID)).
		ThenByAscending(p => p.Name);

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

	[Reactive] public string ModType { get; set; }

	[Reactive] public DateTime? LastUpdated { get; set; }

	[Reactive] public DivinityExtenderModStatus ExtenderModStatus { get; set; }
	[Reactive] public DivinityOsirisModStatus OsirisModStatus { get; set; }

	[Reactive] public int CurrentExtenderVersion { get; set; }

	private string ExtenderStatusToToolTipText(DivinityExtenderModStatus status, int requiredVersion, int currentVersion)
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
			result += "No installed Script Extender version found.\nIf you've already downloaded it, try opening the game once to complete the installation process.";
		}
		return result;
	}

	[Reactive] public DivinityModScriptExtenderConfig ScriptExtenderData { get; set; }

	public SourceList<ModuleShortDesc> Dependencies { get; set; } = new SourceList<ModuleShortDesc>();
	public SourceList<ModuleShortDesc> Conflicts { get; set; } = new SourceList<ModuleShortDesc>();


	protected ReadOnlyObservableCollection<ModuleShortDesc> displayedDependencies;
	public ReadOnlyObservableCollection<ModuleShortDesc> DisplayedDependencies => displayedDependencies;

	protected ReadOnlyObservableCollection<ModuleShortDesc> displayedConflicts;
	public ReadOnlyObservableCollection<ModuleShortDesc> DisplayedConflicts => displayedConflicts;

	public override string GetDisplayName()
	{
		if (DisplayFileForName)
		{
			if (!IsEditorMod)
			{
				return FileName;
			}
			else
			{
				return Folder + " [Editor Project]";
			}
		}
		else
		{
			if (!DivinityApp.DeveloperModeEnabled && UUID == DivinityApp.MAIN_CAMPAIGN_UUID)
			{
				return "Main";
			}
			return Name;
		}
	}

	private readonly ObservableAsPropertyHelper<bool> hasToolTip;

	public bool HasToolTip => hasToolTip.Value;

	[ObservableAsProperty] public int TotalDependencies { get; }
	[ObservableAsProperty] public bool HasDependencies { get; }

	[ObservableAsProperty] public int TotalConflicts { get; }
	[ObservableAsProperty] public bool HasConflicts { get; }

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

	private readonly ObservableAsPropertyHelper<bool> _canDelete;
	public bool CanDelete => _canDelete.Value;

	private readonly ObservableAsPropertyHelper<bool> _canAddToLoadOrder;
	public bool CanAddToLoadOrder => _canAddToLoadOrder.Value;

	private readonly ObservableAsPropertyHelper<bool> _canOpenWorkshopLink;
	public bool CanOpenWorkshopLink => _canOpenWorkshopLink.Value;

	private readonly ObservableAsPropertyHelper<string> _extenderSupportToolTipText;
	public string ScriptExtenderSupportToolTipText => _extenderSupportToolTipText.Value;

	private readonly ObservableAsPropertyHelper<string> _osirisStatusToolTipText;
	public string OsirisStatusToolTipText => _osirisStatusToolTipText.Value;

	private readonly ObservableAsPropertyHelper<string> _lastModifiedDateText;
	public string LastModifiedDateText => _lastModifiedDateText.Value;

	private readonly ObservableAsPropertyHelper<string> _displayVersion;
	public string DisplayVersion => _displayVersion.Value;

	[ObservableAsProperty] public Visibility DependencyVisibility { get; }
	[ObservableAsProperty] public Visibility ConflictsVisibility { get; }

	private readonly ObservableAsPropertyHelper<Visibility> _openWorkshopLinkVisibility;
	public Visibility OpenWorkshopLinkVisibility => _openWorkshopLinkVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _openNexusModsLinkVisibility;
	public Visibility OpenNexusModsLinkVisibility => _openNexusModsLinkVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _toggleForceAllowInLoadOrderVisibility;
	public Visibility ToggleForceAllowInLoadOrderVisibility => _toggleForceAllowInLoadOrderVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _extenderStatusVisibility;
	public Visibility ExtenderStatusVisibility => _extenderStatusVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _osirisStatusVisibility;
	public Visibility OsirisStatusVisibility => _osirisStatusVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _hasFilePathVisibility;
	public Visibility HasFilePathVisibility => _hasFilePathVisibility.Value;

	#region NexusMods Properties

	private readonly ObservableAsPropertyHelper<bool> _canOpenNexusModsLink;
	public bool CanOpenNexusModsLink => _canOpenNexusModsLink.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _nexusImageVisibility;
	public Visibility NexusImageVisibility => _nexusImageVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _nexusModsInformationVisibility;
	public Visibility NexusModsInformationVisibility => _nexusModsInformationVisibility.Value;

	private readonly ObservableAsPropertyHelper<DateTime> _nexusModsCreatedDate;
	public DateTime NexusModsCreatedDate => _nexusModsCreatedDate.Value;

	private readonly ObservableAsPropertyHelper<DateTime> _nexusModsUpdatedDate;
	public DateTime NexusModsUpdatedDate => _nexusModsUpdatedDate.Value;

	private readonly ObservableAsPropertyHelper<string> _nexusModsTooltipInfo;
	public string NexusModsTooltipInfo => _nexusModsTooltipInfo.Value;

	#endregion

	[Reactive] public bool WorkshopEnabled { get; set; }
	[Reactive] public bool NexusModsEnabled { get; set; }
	[Reactive] public bool CanDrag { get; set; }
	[Reactive] public bool DeveloperMode { get; set; }
	[Reactive] public bool HasColorOverride { get; set; }
	[Reactive] public string SelectedColor { get; set; }
	[Reactive] public string ListColor { get; set; }

	public HashSet<string> Files { get; set; }

	[Reactive] public DivinityModWorkshopData WorkshopData { get; set; }
	[Reactive] public NexusModsModData NexusModsData { get; set; }
	[Reactive] public GithubModData GithubData { get; set; }

	public string GetURL(ModSourceType modSourceType, bool asProtocol = false)
	{
		switch (modSourceType)
		{
			case ModSourceType.STEAM:
				if (WorkshopData != null && WorkshopData.ID != "")
				{
					if (!asProtocol)
					{
						return $"https://steamcommunity.com/sharedfiles/filedetails/?id={WorkshopData.ID}";
					}
					else
					{
						return $"steam://url/CommunityFilePage/{WorkshopData.ID}";
					}
				}
				break;
			case ModSourceType.NEXUSMODS:
				if (NexusModsData != null && NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START)
				{
					return String.Format(DivinityApp.NEXUSMODS_MOD_URL, NexusModsData.ModId);
				}
				break;
			case ModSourceType.GITHUB:
				if (GithubData != null)
				{
					return $"https://github.com/{GithubData.Author}/{GithubData.Repository}";
				}
				break;
		}
		return "";
	}

	public List<string> GetAllURLs(bool asProtocol = false)
	{
		var urls = new List<string>();
		var steamUrl = GetURL(ModSourceType.STEAM, asProtocol);
		if (!String.IsNullOrEmpty(steamUrl))
		{
			urls.Add(steamUrl);
		}
		var nexusUrl = GetURL(ModSourceType.NEXUSMODS, asProtocol);
		if (!String.IsNullOrEmpty(nexusUrl))
		{
			urls.Add(nexusUrl);
		}
		var githubUrl = GetURL(ModSourceType.GITHUB, asProtocol);
		if (!String.IsNullOrEmpty(githubUrl))
		{
			urls.Add(githubUrl);
		}
		return urls;
	}

	public override string ToString()
	{
		return $"Name({Name}) Version({Version?.Version}) Author({Author}) UUID({UUID})";
	}

	public DivinityLoadOrderEntry ToOrderEntry()
	{
		return new DivinityLoadOrderEntry
		{
			UUID = this.UUID,
			Name = this.Name
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

	private string OsirisStatusToTooltipText(DivinityOsirisModStatus status)
	{
		switch (status)
		{
			case DivinityOsirisModStatus.SCRIPTS:
				return "Has Osiris Scripting";
			case DivinityOsirisModStatus.MODFIXER:
				return "Has Mod Fixer";
			case DivinityOsirisModStatus.NONE:
			default:
				return "";
		}
	}

	private bool CanOpenWorkshopBoolCheck(bool enabled, bool isHidden, bool isLarianMod, string workshopID)
	{
		return enabled && !isHidden & !isLarianMod & !String.IsNullOrEmpty(workshopID);
	}

	private string NexusModsInfoToTooltip(DateTime createdDate, DateTime updatedDate, long endorsements)
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

		return String.Join("\n", lines);
	}

	public DivinityModData(bool isBaseGameMod = false) : base()
	{
		Index = -1;
		CanDrag = true;

		WorkshopData = new DivinityModWorkshopData();
		NexusModsData = new NexusModsModData();
		//GithubData = new GithubModData();

		this.WhenAnyValue(x => x.UUID).BindTo(NexusModsData, x => x.UUID);

		_nexusImageVisibility = this.WhenAnyValue(x => x.NexusModsData.PictureUrl)
			.Select(uri => uri != null && !String.IsNullOrEmpty(uri.AbsolutePath) ? Visibility.Visible : Visibility.Collapsed)
			.StartWith(Visibility.Collapsed)
			.ToProperty(this, nameof(NexusImageVisibility), scheduler: RxApp.MainThreadScheduler);

		_nexusModsInformationVisibility = this.WhenAnyValue(x => x.NexusModsData.IsUpdated)
			.Select(b => b ? Visibility.Visible : Visibility.Collapsed)
			.StartWith(Visibility.Collapsed)
			.ToProperty(this, nameof(NexusModsInformationVisibility), scheduler: RxApp.MainThreadScheduler);

		_nexusModsCreatedDate = this.WhenAnyValue(x => x.NexusModsData.CreatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToProperty(this, nameof(NexusModsCreatedDate));
		_nexusModsUpdatedDate = this.WhenAnyValue(x => x.NexusModsData.UpdatedTimestamp).SkipWhile(x => x <= 0).Select(x => DateUtils.UnixTimeStampToDateTime(x)).ToProperty(this, nameof(NexusModsUpdatedDate));

		_nexusModsTooltipInfo = this.WhenAnyValue(x => x.NexusModsCreatedDate, x => x.NexusModsUpdatedDate, x => x.NexusModsData.EndorsementCount)
			.Select(x => NexusModsInfoToTooltip(x.Item1, x.Item2, x.Item3)).ToProperty(this, nameof(NexusModsTooltipInfo));

		_toggleForceAllowInLoadOrderVisibility = this.WhenAnyValue(x => x.IsForceLoaded, x => x.HasMetadata, x => x.IsForceLoadedMergedMod)
			.Select(b => b.Item1 && b.Item2 && !b.Item3 ? Visibility.Visible : Visibility.Collapsed)
			.StartWith(Visibility.Collapsed)
			.ToProperty(this, nameof(ToggleForceAllowInLoadOrderVisibility), scheduler: RxApp.MainThreadScheduler);

		_canOpenWorkshopLink = this.WhenAnyValue(x => x.WorkshopEnabled, x => x.IsHidden, x => x.IsLarianMod, x => x.WorkshopData.ID, CanOpenWorkshopBoolCheck).ToProperty(this, nameof(CanOpenWorkshopLink));
		_openWorkshopLinkVisibility = this.WhenAnyValue(x => x.CanOpenWorkshopLink)
			.Select(b => b ? Visibility.Visible : Visibility.Collapsed)
			.StartWith(Visibility.Collapsed)
			.ToProperty(this, nameof(OpenWorkshopLinkVisibility), scheduler: RxApp.MainThreadScheduler);

		_canOpenNexusModsLink = this.WhenAnyValue(x => x.NexusModsEnabled, x => x.NexusModsData.ModId, (b, id) => b && id >= DivinityApp.NEXUSMODS_MOD_ID_START).ToProperty(this, nameof(CanOpenNexusModsLink));
		_openNexusModsLinkVisibility = this.WhenAnyValue(x => x.CanOpenNexusModsLink)
			.Select(b => b ? Visibility.Visible : Visibility.Collapsed)
			.StartWith(Visibility.Collapsed)
			.ToProperty(this, nameof(OpenNexusModsLinkVisibility), scheduler: RxApp.MainThreadScheduler);

		var depConn = this.Dependencies.Connect().ObserveOn(RxApp.MainThreadScheduler);
		depConn.Sort(_moduleSort).Bind(out displayedDependencies).DisposeMany().Subscribe();
		depConn.Count().ToUIPropertyImmediate(this, x => x.TotalDependencies);
		this.WhenAnyValue(x => x.TotalDependencies, c => c > 0).ToUIPropertyImmediate(this, x => x.HasDependencies);
		this.WhenAnyValue(x => x.HasDependencies)
			.Select(PropertyConverters.BoolToVisibility).StartWith(Visibility.Collapsed)
			.ToUIProperty(this, x => x.DependencyVisibility);

		var conConn = this.Conflicts.Connect().ObserveOn(RxApp.MainThreadScheduler);
		conConn.Sort(_moduleSort).Bind(out displayedConflicts).DisposeMany().Subscribe();
		conConn.Count().ToUIPropertyImmediate(this, x => x.TotalConflicts);
		this.WhenAnyValue(x => x.TotalConflicts, c => c > 0).ToUIPropertyImmediate(this, x => x.HasConflicts);
		this.WhenAnyValue(x => x.HasConflicts)
			.Select(PropertyConverters.BoolToVisibility).StartWith(Visibility.Collapsed)
			.ToUIProperty(this, x => x.ConflictsVisibility);

		this.WhenAnyValue(x => x.IsActive, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod,
			x => x.ForceAllowInLoadOrder).Subscribe((b) =>
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

		this.WhenAnyValue(x => x.IsForceLoaded, x => x.IsEditorMod).Subscribe((b) =>
		{
			var isForceLoaded = b.Item1;
			var isEditorMod = b.Item2;

			if (isForceLoaded)
			{
				this.SelectedColor = "#64F38F00";
				this.ListColor = "#32C17200";
				HasColorOverride = true;
			}
			else if (isEditorMod)
			{
				this.SelectedColor = "#6400ED48";
				this.ListColor = "#0C00FF4D";
				HasColorOverride = true;
			}
			else
			{
				HasColorOverride = false;
			}
		});

		if (isBaseGameMod)
		{
			this.IsHidden = UUID != DivinityApp.MAIN_CAMPAIGN_UUID;
			this.IsLarianMod = true;
		}

		// If a screen reader is active, don't bother making tooltips for the mod item entry
		hasToolTip = this.WhenAnyValue(x => x.Description, x => x.HasDependencies, x => x.UUID).
			Select(x => !DivinityApp.IsScreenReaderActive() && (
			!String.IsNullOrEmpty(x.Item1) || x.Item2 || !String.IsNullOrEmpty(x.Item3))).StartWith(true).ToProperty(this, nameof(HasToolTip));

		_canDelete = this.WhenAnyValue(x => x.IsEditorMod, x => x.IsLarianMod, x => x.FilePath,
			(isEditorMod, isLarianMod, path) => !isEditorMod && !isLarianMod && File.Exists(path)).ToProperty(this, nameof(CanDelete));
		_canAddToLoadOrder = this.WhenAnyValue(x => x.ModType, x => x.IsLarianMod, x => x.IsForceLoaded, x => x.IsForceLoadedMergedMod, x => x.ForceAllowInLoadOrder,
			(modType, isLarianMod, isForceLoaded, isMergedMod, forceAllowInLoadOrder) => modType != "Adventure" && !isLarianMod && (!isForceLoaded || isMergedMod) || forceAllowInLoadOrder).StartWith(true).ToProperty(this, nameof(CanAddToLoadOrder));

		var whenExtenderProp = this.WhenAnyValue(x => x.ExtenderModStatus, x => x.ScriptExtenderData.RequiredVersion, x => x.CurrentExtenderVersion);
		_extenderSupportToolTipText = whenExtenderProp.Select(x => ExtenderStatusToToolTipText(x.Item1, x.Item2, x.Item3)).ToProperty(this, nameof(ScriptExtenderSupportToolTipText), true, RxApp.MainThreadScheduler);
		_extenderStatusVisibility = this.WhenAnyValue(x => x.ExtenderModStatus).Select(x => x != DivinityExtenderModStatus.NONE ? Visibility.Visible : Visibility.Collapsed).ToProperty(this, nameof(ExtenderStatusVisibility), true, RxApp.MainThreadScheduler);

		var whenOsirisStatusChanges = this.WhenAnyValue(x => x.OsirisModStatus);
		_osirisStatusVisibility = whenOsirisStatusChanges.Select(x => x != DivinityOsirisModStatus.NONE ? Visibility.Visible : Visibility.Collapsed).ToProperty(this, nameof(OsirisStatusVisibility), true, RxApp.MainThreadScheduler);
		_osirisStatusToolTipText = whenOsirisStatusChanges.Select(OsirisStatusToTooltipText).ToProperty(this, nameof(OsirisStatusToolTipText), true, RxApp.MainThreadScheduler);
		ExtenderModStatus = DivinityExtenderModStatus.NONE;
		OsirisModStatus = DivinityOsirisModStatus.NONE;

		_lastModifiedDateText = this.WhenAnyValue(x => x.LastUpdated).SkipWhile(x => !x.HasValue)
			.Select(x => $"Last Modified on {x.Value.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture)}")
			.StartWith("")
			.ToProperty(this, nameof(LastModifiedDateText), true, RxApp.MainThreadScheduler);

		_hasFilePathVisibility = this.WhenAnyValue(x => x.FilePath).Select(x => !String.IsNullOrEmpty(x) ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Collapsed).ToProperty(this, nameof(HasFilePathVisibility), true, RxApp.MainThreadScheduler);
		_displayVersion = this.WhenAnyValue(x => x.Version.Version).StartWith("0.0.0.0").ToProperty(this, nameof(DisplayVersion), true, RxApp.MainThreadScheduler);
	}
}
