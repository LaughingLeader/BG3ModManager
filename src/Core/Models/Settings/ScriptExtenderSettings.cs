using ModManager.Extensions;

using System.ComponentModel;
using System.Runtime.Serialization;

namespace ModManager.Models.Settings;

[DataContract]
public class ScriptExtenderSettings : ReactiveObject
{
	[Reactive] public bool DevOptionsEnabled { get; set; }
	[Reactive] public bool ExtenderIsAvailable { get; set; }
	[Reactive] public bool ExtenderUpdaterIsAvailable { get; set; }
	[Reactive] public string? ExtenderVersion { get; set; }
	[Reactive] public int ExtenderMajorVersion { get; set; }

	[SettingsEntry("Export Default Values", "Export all values, even if it matches a default extender value")]
	[DataMember, Reactive]
	[JsonIgnore] // This isn't an actual extender setting, so omit it from the exported json
	[DefaultValue(false)]
	public bool ExportDefaultExtenderSettings { get; set; }

	[SettingsEntry("Enable Developer Mode", "Enables various debug functionality for development purposes\nThis can be checked by mods to enable additional log messages and more")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DeveloperMode { get; set; }

	[SettingsEntry("Custom Profile", "Use a profile other than Public\nThis should be the profile folder name")]
	[DataMember, Reactive]
	[DefaultValue("")]
	public string? CustomProfile { get; set; }

	[SettingsEntry("Create Console Window", "Creates a console window that logs extender internals\nMainly useful for debugging")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool CreateConsole { get; set; }

	[SettingsEntry("Log Working Story Errors", "Log errors during Osiris story compilation to a log file (LogFailedCompile)")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool LogFailedCompile { get; set; }

	[SettingsEntry("Enable Osiris Logging", "Enable logging of Osiris activity (rule evaluation, queries, etc.) to a log file")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool EnableLogging { get; set; }

	[SettingsEntry("Log Script Compilation", "Log Osiris story compilation to a log file")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool LogCompile { get; set; }

	[SettingsEntry("Log Directory", "Directory where the generated Osiris logs will be stored\nDefault is Documents\\OsirisLogs")]
	[DataMember, Reactive]
	[DefaultValue("")]
	public string? LogDirectory { get; set; }

	[SettingsEntry("Log Runtime", "Log extender console and script output to a log file")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool LogRuntime { get; set; }

	[SettingsEntry("Disable Launcher", "Prevents the exe from force-opening the launcher\nMay not work correctly if extender auto-updating is enabled, or the --skip-launcher launch param is set", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DisableLauncher { get; set; }

	[SettingsEntry("Disable Story Merge", "Prevents story.div.osi merging, which automatically happens when mods are present\nMay only occur when loading a save", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DisableStoryMerge { get; set; }

	[SettingsEntry("Disable Story Patching", "Prevents patching story.bin with story.div.osi when loading saves, effectively preventing the Osiris scripts in the save from updating", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DisableStoryPatching { get; set; }

	[SettingsEntry("Disable Mod Validation", "Disable module hashing when loading mods\nSpeeds up mod loading with no drawbacks")]
	[DataMember, Reactive]
	[DefaultValue(true)]
	public bool DisableModValidation { get; set; }

	[SettingsEntry("Enable Achievements", "Re-enable achievements for modded games")]
	[DataMember, Reactive]
	[DefaultValue(true)]
	public bool EnableAchievements { get; set; }

	[SettingsEntry("Enable Extensions", "Enables or disables extender API functionality", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(true)]
	public bool EnableExtensions { get; set; }

	[SettingsEntry("Send Crash Reports", "Upload minidumps to the crash report collection server after a game crash")]
	[DataMember, Reactive]
	[DefaultValue(true)]
	public bool SendCrashReports { get; set; }

	[SettingsEntry("Enable Osiris Debugger", "Enables the Osiris debugger interface (vscode extension)", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool EnableDebugger { get; set; }

	[SettingsEntry("Osiris Debugger Port", "Port number the Osiris debugger will listen on\nDefault: 9999", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(9999)]
	public int DebuggerPort { get; set; }

	[SettingsEntry("Dump Network Strings", "Dumps the NetworkFixedString table to LogDirectory\nMainly useful for debugging desync issues", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DumpNetworkStrings { get; set; }

	[SettingsEntry("Osiris Debugger Flags", "Debugger flags to set\nDefault: 0")]
	[DataMember, Reactive]
	[DefaultValue(0)]
	public int DebuggerFlags { get; set; }

	[SettingsEntry("Enable Lua Debugger", "Enables the Lua debugger interface (vscode extension)", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool EnableLuaDebugger { get; set; }

	[SettingsEntry("Lua Builtin Directory", "An additional directory where the Script Extender will check for builtin scripts\nThis setting is meant for developers, to make it easier to test builtin script changes", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue("")]
	public string? LuaBuiltinResourceDirectory { get; set; }

	[SettingsEntry("Clear Console On Reset", "Clears the extender console when the reset command is used", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool ClearOnReset { get; set; }

	[SettingsEntry("Default to Client Side", "Defaults the extender console to the client-side\nThis is setting is intended for developers", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DefaultToClientConsole { get; set; }

	[SettingsEntry("Show Performance Warnings", "Print warnings to the extender console window, which indicates when the server-side part of the game lags behind (a.k.a. warnings about ticks taking too long).", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool ShowPerfWarnings { get; set; }

	[SettingsEntry("Disable ModCrashSanityCheck", "Disables the ModCrashSanityCheck jank that disables mods the next time the game runs")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool InsanityCheck { get; set; }

	public ScriptExtenderSettings()
	{
		this.SetToDefault();
		ExtenderVersion = string.Empty;
		ExtenderMajorVersion = -1;
	}
}
