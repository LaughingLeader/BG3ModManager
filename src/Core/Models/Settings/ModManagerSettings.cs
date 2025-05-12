using DynamicData;
using DynamicData.Binding;

using ModManager.Util;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace ModManager.Models.Settings;

[DataContract]
public class ModManagerSettings : BaseSettings<ModManagerSettings>, ISerializableSettings
{
	[SettingsEntry("Game Data Path", "The path to the Data folder\nExample: Baldur's Gate 3/Data")]
	[DataMember, Reactive] public string? GameDataPath { get; set; }

	[SettingsEntry("Game Executable Path", "The path to bg3.exe")]
	[DataMember, Reactive] public string? GameExecutablePath { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("DirectX 11", "If enabled, when launching the game, bg3_dx11.exe is used instead")]
	[DataMember, Reactive] public bool LaunchDX11 { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Story Log", "When launching the game, enable the Osiris story log (osiris.log)")]
	[DataMember, Reactive] public bool GameStoryLogEnabled { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Launcher - Disable Telemetry", "Disable the telemetry options in the launcher\nTelemetry is always disabled if mods are active")]
	[DataMember, Reactive] public bool DisableLauncherTelemetry { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Launcher - Disable Warnings", "Disable the mod/data mismatch warnings in the launcher")]
	[DataMember, Reactive] public bool DisableLauncherModWarnings { get; set; }

	[DefaultValue(LaunchGameType.Exe)]
	[SettingsEntry("Launch Game - Action", "Change how to launch the game", bindTo: nameof(LaunchTypeIndex))]
	[DataMember, Reactive] public LaunchGameType LaunchType { get; set; }

	[DefaultValue("")]
	[SettingsEntry("Launch Game - Custom Action", "A file path, protocol, or custom process shell command to run", bindVisibilityTo: nameof(IsCustomLaunchEnabled))]
	[DataMember, Reactive] public string? CustomLaunchAction { get; set; }

	[DefaultValue("")]
	[SettingsEntry("Launch Game - Custom Arguments", "Optional additional arguments to path to the custom launch command", bindVisibilityTo: nameof(IsCustomLaunchEnabled))]
	[DataMember, Reactive] public string? CustomLaunchArgs { get; set; }

	[DefaultValue("Orders")]
	[SettingsEntry("Load Orders Path", "The folder containing mod load order .json files")]
	[DataMember, Reactive] public string? LoadOrderPath { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Internal Logging", "Enable the log for the mod manager", disableAutoGen: true)]
	[DataMember, Reactive] public bool LogEnabled { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Add Missing Dependencies When Exporting", "Automatically add dependency mods above their dependents in the exported load order, if omitted from the active order")]
	[DataMember, Reactive] public bool AutoAddDependenciesWhenExporting { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Automatically Check For Updates", "Automatically check for updates when the program starts")]
	[DataMember, Reactive] public bool CheckForUpdates { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Limit to Single Instance", "Prevent the mod manager from launching multiple instances of the game\nThis can be bypassed by holding Shift when clicking on the launch button")]
	[DataMember, Reactive] public bool LimitToSingleInstance { get; set; }

	[DefaultValue("")]
	[SettingsEntry("Override AppData Path", "[EXPERIMENTAL]\nOverride the default location to %LOCALAPPDATA%\\Larian Studios\\Baldur's Gate 3\nThis folder is used when exporting load orders, loading profiles, and loading mods.")]
	[DataMember, Reactive] public string? DocumentsFolderPathOverride { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Colorblind Support", "Enables some colorblind support, such as displaying icons for toolkit projects (which normally have a green background)")]
	[DataMember, Reactive] public bool EnableColorblindSupport { get; set; }

	[DefaultValue(true)]
	[DataMember, Reactive] public bool DarkThemeEnabled { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Shift Focus on Swap", "When moving selected mods to the opposite list with Enter, move focus to that list as well")]
	[DataMember, Reactive] public bool ShiftListFocusOnSwap { get; set; }

	[DefaultValue(GameLaunchWindowAction.None)]
	[SettingsEntry("On Game Launch", "When the game launches through the mod manager, this action will be performed", bindTo: nameof(ActionOnGameLaunchIndex))]
	[DataMember, Reactive]
	public GameLaunchWindowAction ActionOnGameLaunch { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Skip Checking for Missing Mods", "If a load order is missing mods, no warnings will be displayed")]
	[DataMember, Reactive] public bool DisableMissingModWarnings { get; set; }

	[DefaultValue(false)]
	[Reactive] public bool DisplayFileNames { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Mod Developer Mode", "This enables features for mod developers, such as being able to copy a mod's UUID in context menus, and additional Script Extender options", disableAutoGen: true)]
	[Reactive, DataMember] public bool DebugModeEnabled { get; set; }

	[DefaultValue("")]
	[DataMember, Reactive] public string? GameLaunchParams { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Save Window Location", "Save and restore the window location when the application starts.")]
	[DataMember, Reactive] public bool SaveWindowLocation { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Delete ModCrashSanityCheck", "Automatically delete the %LOCALAPPDATA%/Larian Studios/Baldur's Gate 3/ModCrashSanityCheck folder, which may make certain mods deactivate if it exists")]
	[DataMember, Reactive] public bool DeleteModCrashSanityCheck { get; set; }

	[DataMember, Reactive] public long LastUpdateCheck { get; set; }
	[DataMember, Reactive] public string? LastOrder { get; set; }
	[DataMember, Reactive] public string? LastImportDirectoryPath { get; set; }
	[DataMember, Reactive] public string? LastLoadedOrderFilePath { get; set; }
	[DataMember, Reactive] public string? LastExtractOutputPath { get; set; }

	[DataMember, Reactive] public ModManagerUpdateSettings UpdateSettings { get; set; }
	[DataMember, Reactive] public WindowSettings Window { get; set; }
	[DataMember, Reactive] public ConfirmationSettings Confirmations { get; set; }

	[Reactive] public bool Loaded { get; set; }
	[Reactive] public bool SettingsWindowIsOpen { get; set; }

	[Reactive] public string? DefaultExtenderLogDirectory { get; set; }
	[Reactive] public string? ExtenderLogDirectory { get; set; }

	[Reactive] public int LaunchTypeIndex { get; set; }
	[Reactive] public int ActionOnGameLaunchIndex { get; set; }

	[ObservableAsProperty] public bool IsCustomLaunchEnabled { get; }

	[JsonExtensionData]
	private IDictionary<string, object> AdditionalFields { get; set; } = new Dictionary<string, object>();

	private static bool TryGetExtraProperty<T>(IDictionary<string, object> additionalProperties, string key, [NotNullWhen(true)] out T? value)
	{
		value = default;
		if (additionalProperties.TryGetValue(key, out var entryObj) && entryObj is T entry)
		{
			value = entry;
			return value != null;
		}
		return false;
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext context)
	{
		if (TryGetExtraProperty(AdditionalFields, "LaunchThroughSteam", out bool launchThroughSteam) && launchThroughSteam == true)
		{
			LaunchType = LaunchGameType.Steam;
		}
	}

	public ModManagerSettings() : base("settings.json")
	{
		this.SetToDefault();

		UpdateSettings = new();
		Window = new();
		Confirmations = new();
	}
}
