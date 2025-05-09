using DynamicData;
using DynamicData.Binding;

using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace ModManager.Models.Settings;

[DataContract]
public class ModManagerSettings : BaseSettings<ModManagerSettings>, ISerializableSettings
{
	[SettingsEntry("Game Data Path", "The path to the Data folder, for loading editor mods\nExample: Baldur's Gate 3/Data")]
	[DataMember, Reactive] public string? GameDataPath { get; set; }

	[SettingsEntry("Game Executable Path", "The path to bg3.exe")]
	[DataMember, Reactive] public string? GameExecutablePath { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Enable Story Log", "When launching the game, enable the Osiris story log (osiris.log)")]
	[DataMember, Reactive] public bool GameStoryLogEnabled { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Launcher - Disable Telemetry", "Disable the telemetry options in the launcher\nTelemetry is always disabled if mods are active")]
	[DataMember, Reactive] public bool DisableLauncherTelemetry { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Launcher - Disable Warnings", "Disable the mod/data mismatch warnings in the launcher")]
	[DataMember, Reactive] public bool DisableLauncherModWarnings { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("DirectX 11", "If enabled, when launching the game, bg3_dx11.exe is used instead")]
	[DataMember, Reactive] public bool LaunchDX11 { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Steam - Skip Launcher", "Creates a steam_appid.txt in the bin folder if it doesn't exist, allowing you to bypassing the launcher when running the game exe directly")]
	[DataMember, Reactive] public bool SkipLauncher { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Launch Through Steam", "Launch the game through steam, instead of by the exe directly")]
	[DataMember, Reactive] public bool LaunchThroughSteam { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Limit to Single Instance", "Prevent the mod manager from launching multiple instances of the game\nThis can be bypassed by holding Shift when clicking on the launch button")]
	[DataMember, Reactive] public bool LimitToSingleInstance { get; set; }

	[DefaultValue("Orders")]
	[SettingsEntry("Load Orders Path", "The folder containing mod load order .json files")]
	[DataMember, Reactive] public string? LoadOrderPath { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Internal Logging", "Enable the log for the mod manager", DisableAutoGen = true)]
	[DataMember, Reactive] public bool LogEnabled { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Add Missing Dependencies When Exporting", "Automatically add dependency mods above their dependents in the exported load order, if omitted from the active order")]
	[DataMember, Reactive] public bool AutoAddDependenciesWhenExporting { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Automatically Check For Updates", "Automatically check for updates when the program starts")]
	[DataMember, Reactive] public bool CheckForUpdates { get; set; }

	[DefaultValue("")]
	[SettingsEntry("Override AppData Path", "[EXPERIMENTAL]\nOverride the default location to %LOCALAPPDATA%\\Larian Studios\\Baldur's Gate 3\nThis folder is used when exporting load orders, loading profiles, and loading mods")]
	[DataMember, Reactive] public string? DocumentsFolderPathOverride { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Shift Focus on Swap", "When moving selected mods to the opposite list with Enter, move focus to that list as well")]
	[DataMember, Reactive] public bool ShiftListFocusOnSwap { get; set; }

	[DataMember, Reactive]
	[SettingsEntry("On Game Launch", "When the game launches through the mod manager, this action will be performed", nameof(ActionOnGameLaunchIndex))]
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public GameLaunchWindowAction ActionOnGameLaunch { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Skip Checking for Missing Mods", "If a load order is missing mods, no warnings will be displayed")]
	[DataMember, Reactive] public bool DisableMissingModWarnings { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Mod Developer Mode", "This enables features for mod developers, such as being able to copy a mod's UUID in context menus, and additional Script Extender options", DisableAutoGen = true)]
	[DataMember, Reactive]
	public bool DebugModeEnabled { get; set; }

	[DefaultValue(false)]
	[Reactive] public bool DisplayFileNames { get; set; }

	[DefaultValue(0)]
	[Reactive]
	public int ActionOnGameLaunchIndex { get; set; }

	[DefaultValue("")]
	[DataMember, Reactive] public string? GameLaunchParams { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Save Window Location", "Save and restore the window location when the application starts.")]
	[DataMember, Reactive] public bool SaveWindowLocation { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Delete ModCrashSanityCheck", "Automatically delete the %LOCALAPPDATA%/Larian Studios/Baldur's Gate 3/ModCrashSanityCheck folder,\nwhich may make certain mods deactivate if it exists")]
	[DataMember][Reactive] public bool DeleteModCrashSanityCheck { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Colorblind Support", "Enables some colorblind support, such as displaying icons for toolkit projects (which normally have a green background)")]
	[DataMember, Reactive] public bool EnableColorblindSupport { get; set; }

	[DefaultValue(true)]
	[DataMember, Reactive] public bool DarkThemeEnabled { get; set; }

	[DataMember, Reactive] public long LastUpdateCheck { get; set; }
	[DataMember, Reactive] public string? LastOrder { get; set; }
	[DataMember, Reactive] public string? LastImportDirectoryPath { get; set; }
	[DataMember, Reactive] public string? LastLoadedOrderFilePath { get; set; }
	[DataMember, Reactive] public string? LastExtractOutputPath { get; set; }

	[DataMember, Reactive] public ScriptExtenderSettings ExtenderSettings { get; set; }
	[DataMember, Reactive] public ScriptExtenderUpdateConfig ExtenderUpdaterSettings { get; set; }
	[DataMember, Reactive] public ModManagerUpdateSettings UpdateSettings { get; set; }
	[DataMember, Reactive] public WindowSettings Window { get; set; }

	[Reactive] public bool Loaded { get; set; }
	[Reactive] public bool CanSaveSettings { get; set; }
	[Reactive] public bool SettingsWindowIsOpen { get; set; }

	[Reactive] public string? DefaultExtenderLogDirectory { get; set; }
	[Reactive] public string? ExtenderLogDirectory { get; set; }

	private static string? GetExtenderLogsDirectory(string? defaultDirectory, string? logDirectory)
	{
		if (!logDirectory.IsValid())
		{
			return defaultDirectory;
		}
		return logDirectory;
	}

	public void InitSubscriptions()
	{
		var properties = typeof(ModManagerSettings)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		this.WhenAnyPropertyChanged(properties).Subscribe((c) =>
		{
			if (SettingsWindowIsOpen) CanSaveSettings = true;
		});

		var updateProperties = typeof(ModManagerUpdateSettings)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		UpdateSettings.WhenAnyPropertyChanged(updateProperties).Subscribe((c) =>
		{
			if (SettingsWindowIsOpen) CanSaveSettings = true;
		});

		var extenderProperties = typeof(ScriptExtenderSettings)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		ExtenderSettings.WhenAnyPropertyChanged(extenderProperties).Subscribe((c) =>
		{
			if (SettingsWindowIsOpen) CanSaveSettings = true;
			this.RaisePropertyChanged(nameof(ExtenderLogDirectory));
		});

		var extenderUpdaterProperties = typeof(ScriptExtenderUpdateConfig)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(DataMemberAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		ExtenderUpdaterSettings.WhenAnyPropertyChanged(extenderUpdaterProperties).Subscribe((c) =>
		{
			if (SettingsWindowIsOpen) CanSaveSettings = true;
		});

		this.WhenAnyValue(x => x.DebugModeEnabled).Subscribe(b => DivinityApp.DeveloperModeEnabled = b);

		this.WhenAnyValue(x => x.ActionOnGameLaunchIndex).Select(x => (GameLaunchWindowAction)x).BindTo(this, x => x.ActionOnGameLaunch);
		this.WhenAnyValue(x => x.ActionOnGameLaunch).Select(x => (int)x).BindTo(this, x => x.ActionOnGameLaunchIndex);

		this.WhenAnyValue(x => x.DebugModeEnabled).BindTo(ExtenderSettings, x => x.DevOptionsEnabled);
		this.WhenAnyValue(x => x.DebugModeEnabled).BindTo(ExtenderUpdaterSettings, x => x.DevOptionsEnabled);

		this.WhenAnyValue(x => x.DefaultExtenderLogDirectory, x => x.ExtenderSettings.LogDirectory)
		.Select(x => GetExtenderLogsDirectory(x.Item1, x.Item2))
		.BindTo(this, x => x.ExtenderLogDirectory);
	}

	public ModManagerSettings() : base("settings.json")
	{
		UpdateSettings = new ModManagerUpdateSettings();
		ExtenderSettings = new ScriptExtenderSettings();
		ExtenderUpdaterSettings = new ScriptExtenderUpdateConfig();
		Window = new WindowSettings();

		this.SetToDefault();
	}
}
