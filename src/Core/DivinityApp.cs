using ModManager.Models.Mod;
using ModManager.Util;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ModManager;

public static class DivinityApp
{
	public const string DIR_DATA = "Data\\";

	public const string URL_DONATION = @"https://ko-fi.com/laughingleader";
	public const string URL_AUTHOR = @"https://github.com/LaughingLeader";

	public const string PATH_RESOURCES = "Resources";
	public const string PATH_APP_FEATURES = "AppFeatures.json";
	public const string PATH_DEFAULT_PATHWAYS = "DefaultPathways.json";
	public const string PATH_IGNORED_MODS = "IgnoredMods.json";

	public const int MAX_FILE_OVERRIDE_DISPLAY = 10;

	public static string DateTimeColumnFormat { get; set; } = "MM/dd/yyyy";
	public static string DateTimeTooltipFormat { get; set; } = "MMMM dd, yyyy";
	public static string DateTimeExtenderBuildFormat { get; set; } = "MM/dd/yyyy hh:mm tt";

#if !DOS2
	public const string PIPE_ID = "bg3mm.server";

	public static readonly HashSet<string> GameExes = ["bg3", "bg3_dx11"];

	public const string HTTP_USER = "BG3ModManagerUser";

	public const string GITHUB_USER = "LaughingLeader";
	public const string GITHUB_REPO = "BG3ModManager";
	public const string GITHUB_RELEASE_ASSET = "BG3ModManager_Latest.zip";

	public const string URL_REPO = @"https://github.com/LaughingLeader/BG3ModManager";
	public const string URL_CHANGELOG = @"https://github.com/LaughingLeader/BG3ModManager/wiki/Changelog";
	public const string URL_CHANGELOG_RAW = @"https://raw.githubusercontent.com/wiki/LaughingLeader/BG3ModManager/Changelog.md";
	public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader/BG3ModManager/master/Update.xml";
	public const string URL_ISSUES = @"https://github.com/LaughingLeader/BG3ModManager/issues";
	public const string URL_LICENSE = @"https://github.com/LaughingLeader/BG3ModManager/blob/master/LICENSE";
	public const string URL_STEAM = @"https://steamcommunity.com/app/1086940";

	public const string XML_MOD_ORDER_MODULE = @"<node id=""Module""><attribute id=""UUID"" type=""guid"" value=""{0}""/></node>";
	public const string XML_MODULE_SHORT_DESC = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" type=""LSString"" value=""{0}""/><attribute id=""MD5"" type=""LSString"" value=""{1}""/><attribute id=""Name"" type=""LSString"" value=""{2}""/><attribute id=""PublishHandle"" type=""uint64"" value=""{5}""/><attribute id=""UUID"" type=""guid"" value=""{3}""/><attribute id=""Version64"" type=""int64"" value=""{4}""/></node>";
	public const string XML_MODULE_SHORT_DESC_FORMATTED = "<node id=\"ModuleShortDesc\">\n\t<attribute id=\"Folder\" type=\"LSString\" value=\"{0}\"/>\n\t<attribute id=\"MD5\" type=\"LSString\" value=\"{1}\"/>\n\t<attribute id=\"Name\" type=\"LSString\" value=\"{2}\"/>\n\t<attribute id=\"PublishHandle\" type=\"uint64\" value=\"{5}\"/>\n\t<attribute id=\"UUID\" type=\"guid\" value=\"{3}\"/>\n\t<attribute id=\"Version64\" type=\"int64\" value=\"{4}\"/>\n</node>";
	public const string XML_MOD_SETTINGS_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?><save><version major=""4"" minor=""7"" revision=""1"" build=""3""/><region id=""ModuleSettings""><node id=""root""><children><node id=""Mods""><children>{0}</children></node></children></node></region></save>";

	public const string MAIN_CAMPAIGN_UUID = "28ac9ce2-2aba-8cda-b3b5-6e922f71b6b8";

	public const string NEXUSMODS_GAME_DOMAIN = "baldursgate3";
	public const long NEXUSMODS_GAME_ID = 3474;
	public const string NEXUSMODS_MOD_URL = "https://www.nexusmods.com/baldursgate3/mods/{0}";
	public const long NEXUSMODS_MOD_ID_START = 1;

	public const string URL_NEXUSMODS = $"https://www.nexusmods.com/{NEXUSMODS_GAME_DOMAIN}";

	public const string MODIO_GAME_DOMAIN = "baldursgate3";
	public const long MODIO_GAME_ID = 6715;
	public const string MODIO_MOD_URL = "https://mod.io/g/baldursgate3/m/{0}";

	public const string EXTENDER_REPO_URL = "Norbyte/bg3se";
	public const string EXTENDER_LATEST_URL = "https://github.com/Norbyte/bg3se/releases/latest";
	public const string EXTENDER_APPDATA_DIRECTORY = "BG3ScriptExtender";
	public const string EXTENDER_APPDATA_DLL = "BG3ScriptExtender.dll";
	public const string EXTENDER_MOD_CONFIG = "ScriptExtender/Config.json";
	public const string EXTENDER_UPDATER_FILE = "DWrite.dll";
	public const string EXTENDER_MANIFESTS_URL = "https://bg3se-updates.norbyte.dev/Channels/{0}/Manifest.json";
	public const string EXTENDER_CONFIG_FILE = "ScriptExtenderSettings.json";
	public const string EXTENDER_UPDATER_CONFIG_FILE = "ScriptExtenderUpdaterConfig.json";
	public const int EXTENDER_DEFAULT_VERSION = 6;

	public const LSLib.LS.Enums.Game GAME = LSLib.LS.Enums.Game.BaldursGate3;
	public const LSLib.LS.Story.Compiler.TargetGame GAME_COMPILER = LSLib.LS.Story.Compiler.TargetGame.BG3;
#else
	public const string PIPE_ID = "divinitymm.server";

	public static readonly HashSet<string> GameExes = ["EoCApp"];

	public const string HTTP_USER = "ModManagerUser";

	public const string URL_REPO = @"https://github.com/LaughingLeader-DOS2-Mods/ModManager";
	public const string URL_CHANGELOG = @"https://github.com/LaughingLeader-DOS2-Mods/ModManager/wiki/Changelog";
	public const string URL_CHANGELOG_RAW = @"https://raw.githubusercontent.com/wiki/LaughingLeader-DOS2-Mods/ModManager/Changelog.md";
	public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader-DOS2-Mods/ModManager/master/Update.xml";
	public const string URL_ISSUES = @"https://github.com/LaughingLeader-DOS2-Mods/ModManager/issues";
	public const string URL_LICENSE = @"https://github.com/LaughingLeader-DOS2-Mods/ModManager/blob/master/LICENSE";

	public const string XML_MOD_ORDER_MODULE = @"<node id=""Module""><attribute id=""UUID"" value=""{0}"" type=""22""/></node>";
	public const string XML_MODULE_SHORT_DESC = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" value=""{0}"" type=""30""/><attribute id=""MD5"" value=""{1}"" type=""23""/><attribute id=""Name"" value=""{2}"" type=""22""/><attribute id=""UUID"" value=""{3}"" type=""22"" /><attribute id=""Version"" value=""{4}"" type=""4""/></node>";
	public const string XML_MODULE_SHORT_DESC_FORMATTED = "<node id=\"ModuleShortDesc\">\n\t<attribute id=\"Folder\" value=\"{0}\" type=\"30\"/>\n\t<attribute id=\"MD5\" value=\"{1}\" type=\"23\"/>\n\t<attribute id=\"Name\" value=\"{2}\" type=\"22\"/>\n\t<attribute id=\"UUID\" value=\"{3}\" type=\"22\" />\n\t<attribute id=\"Version\" value=\"{4}\" type=\"4\"/>\n</node>";
	public const string XML_MOD_SETTINGS_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?><save><header version=""2""/><version major=""3"" minor=""6"" revision=""9"" build=""0""/><region id=""ModuleSettings""><node id=""root""><children><node id=""ModOrder""><children>{0}</children></node><node id=""Mods""><children>{1}</children></node></children></node></region></save>";

	public const string MAIN_CAMPAIGN_UUID = "1301db3d-1f54-4e98-9be5-5094030916e4";
	public const string GAMEMASTER_UUID = "00550ab2-ac92-410c-8d94-742f7629de0e";

	public const string EXTENDER_REPO_URL = "Norbyte/ositools";
	public const string EXTENDER_LATEST_URL = "https://github.com/Norbyte/ositools/releases/latest";
	public const string EXTENDER_APPDATA_DLL_OLD = "OsirisExtender/OsiExtenderEoCApp.dll";
	public const string EXTENDER_MOD_CONFIG = "OsiToolsConfig.json";
	public const string EXTENDER_UPDATER_FILE = "DXGI.dll";
	public const string EXTENDER_APPDATA_DIRECTORY = "DOS2ScriptExtender";
	public const string EXTENDER_APPDATA_DLL = "OsiExtenderEoCPlugin.dll";
	public const string EXTENDER_MANIFESTS_URL = "https://dbn4nit5dt5fw.cloudfront.net/Channels/{0}/Manifest2.json";
	public const string EXTENDER_CONFIG_FILE = "ScriptExtenderSettings.json";
	public const string EXTENDER_UPDATER_CONFIG_FILE = "ScriptExtenderUpdaterConfig.json";
	public const int EXTENDER_DEFAULT_VERSION = 58;

	public const LSLib.LS.Enums.Game GAME = LSLib.LS.Enums.Game.DivinityOriginalSin2DE;
	public const LSLib.LS.Story.Compiler.TargetGame GAME_COMPILER = LSLib.LS.Story.Compiler.TargetGame.DOS2DE;
#endif
	public static HashSet<DivinityModData> IgnoredMods { get; private set; }
	public static HashSet<DivinityModData> IgnoredDependencyMods { get; private set; }

	public static DivinityGlobalEvents Events { get; private set; }

	private static readonly Assembly? _exeAssembly;
	private static readonly string? _exePath;
	private static readonly string? _appDirectory;

	static DivinityApp()
	{
		_exeAssembly = Assembly.GetEntryAssembly();
		_exePath = _exeAssembly?.Location;
		_appDirectory = Path.GetDirectoryName(_exeAssembly?.Location);

		IgnoredMods = [];
		IgnoredDependencyMods = [];
		Events = new DivinityGlobalEvents();

		//Process.GetCurrentProcess()?.MainModule?.FileName
	}

	public static event PropertyChangedEventHandler StaticPropertyChanged;

	private static void NotifyStaticPropertyChanged([CallerMemberName] string? name = null)
	{
		StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));
	}

	private static bool developerModeEnabled = false;

	public static bool DeveloperModeEnabled
	{
		get => developerModeEnabled;
		set
		{
			developerModeEnabled = value;
			NotifyStaticPropertyChanged();
		}
	}

	private static bool _isKeyboardNavigating = false;

	public static bool IsKeyboardNavigating
	{
		get => _isKeyboardNavigating;
		set
		{
			_isKeyboardNavigating = value;
			NotifyStaticPropertyChanged();
		}
	}

	public delegate void LogFunction(string message);

	private static readonly Action<string> BaseLogMethod = s => Trace.WriteLine(s);

	private static Action<string> _overwrittenLogMethod;
	public static Action<string> LogMethod
	{
		get => _overwrittenLogMethod ?? BaseLogMethod;
		set => _overwrittenLogMethod = value;
	}

	public static void Log(string msg, [CallerMemberName] string mName = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = 0)
	{
		LogMethod($"[{Path.GetFileName(path)}:{mName}({line})] {msg}");
	}

	[DllImport("user32.dll")]
	static extern bool SystemParametersInfo(int iAction, int iParam, out bool bActive, int iUpdate);

	public static bool IsScreenReaderActive()
	{
		var iAction = 70; // SPI_GETSCREENREADER constant;
		var iParam = 0;
		var iUpdate = 0;
		var bReturn = SystemParametersInfo(iAction, iParam, out var bActive, iUpdate);
		return bReturn && bActive;
		//if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged) || AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
		//{
		//	return true;
		//}
		//return false;
	}

	public static string GetAppDirectory() => _appDirectory ?? Directory.GetCurrentDirectory();

	public static string GetAppDirectory(params string[] joinPath)
	{
		var exeDir = GetAppDirectory();
		var paths = joinPath.Prepend(exeDir).ToArray();
		return Path.Join(paths);
	}

	public static string? GetExePath() => _exePath;
	public static string GetToolboxPath() => GetAppDirectory("Tools", "Toolbox.exe");

	[Obsolete("Use a direct service reference instead")]
	public static void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		Locator.Current.GetService<IGlobalCommandsService>()?.ShowAlert(message, alertType, timeout);
	}

	[Obsolete("Use a direct service reference instead")]
	public static async Task ShowAlertAsync(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		var commands = Locator.Current.GetService<IGlobalCommandsService>();
		if (commands != null)
		{
			await commands.ShowAlertAsync(message, alertType, timeout);
		}
	}
}
