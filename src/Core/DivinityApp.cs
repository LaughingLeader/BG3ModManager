﻿using DivinityModManager.Models;
using DivinityModManager.Util;

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DivinityModManager;

public static class DivinityApp
{
	public const string DIR_DATA = "Data\\";
	public const string URL_REPO = @"https://github.com/LaughingLeader/BG3ModManager";
	public const string URL_CHANGELOG = @"https://github.com/LaughingLeader/BG3ModManager/wiki/Changelog";
	public const string URL_CHANGELOG_RAW = @"https://raw.githubusercontent.com/wiki/LaughingLeader/BG3ModManager/Changelog.md";
	public const string URL_UPDATE = @"https://raw.githubusercontent.com/LaughingLeader/BG3ModManager/master/Update.xml";
	public const string URL_AUTHOR = @"https://github.com/LaughingLeader";
	public const string URL_ISSUES = @"https://github.com/LaughingLeader/BG3ModManager/issues";
	public const string URL_LICENSE = @"https://github.com/LaughingLeader/BG3ModManager/blob/master/LICENSE";
	public const string URL_DONATION = @"https://ko-fi.com/laughingleader";

	public const string XML_MOD_ORDER_MODULE = @"<node id=""Module""><attribute id=""UUID"" value=""{0}"" type=""FixedString""/></node>";
	public const string XML_MODULE_SHORT_DESC = @"<node id=""ModuleShortDesc""><attribute id=""Folder"" value=""{0}"" type=""LSString""/><attribute id=""MD5"" value=""{1}"" type=""LSString""/><attribute id=""Name"" value=""{2}"" type=""LSString""/><attribute id=""UUID"" value=""{3}"" type=""FixedString"" /><attribute id=""Version64"" value=""{4}"" type=""int64""/></node>";
	public const string XML_MODULE_SHORT_DESC_FORMATTED = "<node id=\"ModuleShortDesc\">\n\t<attribute id=\"Folder\" value=\"{0}\" type=\"LSString\"/>\n\t<attribute id=\"MD5\" value=\"{1}\" type=\"LSString\"/>\n\t<attribute id=\"Name\" value=\"{2}\" type=\"LSString\"/>\n\t<attribute id=\"UUID\" value=\"{3}\" type=\"FixedString\" />\n\t<attribute id=\"Version64\" value=\"{4}\" type=\"int64\"/>\n</node>";
	public const string XML_MOD_SETTINGS_TEMPLATE = @"<?xml version=""1.0"" encoding=""UTF-8""?><save><version major=""4"" minor=""0"" revision=""9"" build=""331""/><region id=""ModuleSettings""><node id=""root""><children><node id=""ModOrder""><children>{0}</children></node><node id=""Mods""><children>{1}</children></node></children></node></region></save>";

	public const string PATH_RESOURCES = "Resources";
	public const string PATH_APP_FEATURES = "AppFeatures.json";
	public const string PATH_DEFAULT_PATHWAYS = "DefaultPathways.json";
	public const string PATH_IGNORED_MODS = "IgnoredMods.json";

	public const string MAIN_CAMPAIGN_UUID = "28ac9ce2-2aba-8cda-b3b5-6e922f71b6b8";
	public const string GAMEMASTER_UUID = "NotYetAvailableInBG3";

	public const string NEXUSMODS_GAME_DOMAIN = "baldursgate3";
	public const long NEXUSMODS_GAME_ID = 3474;
	public const string NEXUSMODS_MOD_URL = "https://www.nexusmods.com/baldursgate3/mods/{0}";
	public const long NEXUSMODS_MOD_ID_START = 1;

	public const long WORKSHOP_MOD_ID_START = 4; // id 5 and up seems to work, while 0-3 are disabled / hidden

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

	public const int MAX_FILE_OVERRIDE_DISPLAY = 10;

	public const LSLib.LS.Enums.Game GAME = LSLib.LS.Enums.Game.BaldursGate3;
	public const LSLib.LS.Story.Compiler.TargetGame GAME_COMPILER = LSLib.LS.Story.Compiler.TargetGame.BG3;

	public const string PIPE_ID = "bg3mm.server";

	public static HashSet<DivinityModData> IgnoredMods { get; private set; }
	public static HashSet<DivinityModData> IgnoredDependencyMods { get; private set; }

	public static DivinityGlobalCommands Commands { get; private set; }
	public static DivinityGlobalEvents Events { get; private set; }

	static DivinityApp()
	{
		IgnoredMods = new HashSet<DivinityModData>();
		IgnoredDependencyMods = new HashSet<DivinityModData>();
		Commands = new DivinityGlobalCommands();
		Events = new DivinityGlobalEvents();
	}

	public static event PropertyChangedEventHandler StaticPropertyChanged;

	private static void NotifyStaticPropertyChanged([CallerMemberName] string name = null)
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

	public static IObservable<Func<DivinityModDependencyData, bool>> DependencyFilter { get; set; }

	public static string DateTimeColumnFormat { get; set; } = "MM/dd/yyyy";
	public static string DateTimeTooltipFormat { get; set; } = "MMMM dd, yyyy";
	public static string DateTimeExtenderBuildFormat { get; set; } = "MM/dd/yyyy hh:mm tt";

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
		int iAction = 70; // SPI_GETSCREENREADER constant;
		int iParam = 0;
		int iUpdate = 0;
		bool bReturn = SystemParametersInfo(iAction, iParam, out bool bActive, iUpdate);
		return bReturn && bActive;
		//if (AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged) || AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged))
		//{
		//	return true;
		//}
		//return false;
	}

	public static string GetAppDirectory()
	{
		return Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
	}

	public static string GetAppDirectory(params string[] joinPath)
	{
		var exeDir = GetAppDirectory();
		var paths = joinPath.Prepend(exeDir).ToArray();
		return Path.Combine(paths);
	}

	public static string GetExePath() => Process.GetCurrentProcess().MainModule.FileName;
	public static string GetToolboxPath() => GetAppDirectory("Tools", "Toolbox.exe");
}
