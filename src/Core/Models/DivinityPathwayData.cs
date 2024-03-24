using ModManager.Extensions;
using ModManager.Models.Settings;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.IO;

namespace ModManager.Models;

public class DivinityPathwayData : ReactiveObject
{
	/// <summary>
	/// The path to the root game folder, i.e. SteamLibrary\steamapps\common\Baldur's Gate 3
	/// </summary>
	[Reactive] public string InstallPath { get; set; }

	/// <summary>
	/// The path to %LOCALAPPDATA%\Larian Studios\Baldur's Gate 3
	/// </summary>
	[Reactive] public string AppDataGameFolder { get; set; }

	/// <summary>
	/// The path to %LOCALAPPDATA%\Larian Studios\Baldur's Gate 3\Mods
	/// </summary>
	[Reactive] public string AppDataModsPath { get; set; }

	/// <summary>
	/// The path to %LOCALAPPDATA%\Larian Studios\Baldur's Gate 3\PlayerProfiles
	/// </summary>
	[Reactive] public string AppDataProfilesPath { get; set; }

	/// <summary>
	/// The path to %LOCALAPPDATA%\Larian Studios\Baldur's Gate 3\DMCampaigns
	/// </summary>
	[Reactive] public string AppDataCampaignsPath { get; set; }

	[Reactive] public string LastSaveFilePath { get; set; }

	[Reactive] public string ScriptExtenderLatestReleaseUrl { get; set; }
	[Reactive] public string ScriptExtenderLatestReleaseVersion { get; set; }

	public DivinityPathwayData()
	{
		InstallPath = "";
		AppDataGameFolder = "";
		AppDataModsPath = "";
		AppDataCampaignsPath = "";
		LastSaveFilePath = "";
		ScriptExtenderLatestReleaseUrl = "";
		ScriptExtenderLatestReleaseVersion = "";
	}

	public string ScriptExtenderSettingsFile(ModManagerSettings settings)
	{
		if (settings.GameExecutablePath.IsExistingFile())
		{
			return Path.Join(Path.GetDirectoryName(settings.GameExecutablePath), DivinityApp.EXTENDER_CONFIG_FILE);
		}
		return "";
	}

	public string ScriptExtenderUpdaterConfigFile(ModManagerSettings settings)
	{
		if (settings.GameExecutablePath.IsExistingFile())
		{
			return Path.Join(Path.GetDirectoryName(settings.GameExecutablePath), DivinityApp.EXTENDER_UPDATER_CONFIG_FILE);
		}
		return "";
	}
}
