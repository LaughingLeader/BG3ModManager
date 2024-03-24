using ModManager.Models;
using ModManager.Util;

using System.IO;

namespace ModManager.Services;
public class PathwaysService : IPathwaysService
{
	private readonly ISettingsService _settingsService;

	public DivinityPathwayData Data { get; }

	public string GetLarianStudiosAppDataFolder()
	{
		if (Directory.Exists(Data.AppDataGameFolder))
		{
			var parentDir = Directory.GetParent(Data.AppDataGameFolder);
			if (parentDir != null)
			{
				return parentDir.FullName;
			}
		}
		string appDataFolder;
		if (!String.IsNullOrEmpty(_settingsService.ManagerSettings.DocumentsFolderPathOverride))
		{
			appDataFolder = _settingsService.ManagerSettings.DocumentsFolderPathOverride;
		}
		else
		{
			appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
			if (String.IsNullOrEmpty(appDataFolder) || !Directory.Exists(appDataFolder))
			{
				var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
				if (Directory.Exists(userFolder))
				{
					appDataFolder = Path.Join(userFolder, "AppData", "Local", "Larian Studios");
				}
			}
			else
			{
				appDataFolder = Path.Join(appDataFolder, "Larian Studios");
			}
		}
		return appDataFolder;
	}

	//TODO Make this work for both DOS2 and BG3
	public bool SetGamePathways(string currentGameDataPath, string gameDataFolderOverride = "")
	{
		try
		{
			var localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

			if (String.IsNullOrWhiteSpace(_settingsService.AppSettings.DefaultPathways.DocumentsGameFolder))
			{
				_settingsService.AppSettings.DefaultPathways.DocumentsGameFolder = "Larian Studios\\Baldur's Gate 3";
			}

			var gameDataFolder = Path.Join(localAppDataFolder, _settingsService.AppSettings.DefaultPathways.DocumentsGameFolder);

			if (!String.IsNullOrEmpty(gameDataFolderOverride) && Directory.Exists(gameDataFolderOverride))
			{
				gameDataFolder = gameDataFolderOverride;
				var parentDir = Directory.GetParent(gameDataFolder);
				if (parentDir != null)
				{
					localAppDataFolder = parentDir.FullName;
				}
			}
			else if (!Directory.Exists(gameDataFolder))
			{
				var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
				if (Directory.Exists(userFolder))
				{
					localAppDataFolder = Path.Join(userFolder, "AppData", "Local");
					gameDataFolder = Path.Join(localAppDataFolder, _settingsService.AppSettings.DefaultPathways.DocumentsGameFolder);
				}
			}

			var modPakFolder = Path.Join(gameDataFolder, "Mods");
			var gmCampaignsFolder = Path.Join(gameDataFolder, "GMCampaigns");
			var profileFolder = Path.Join(gameDataFolder, "PlayerProfiles");

			Data.AppDataGameFolder = gameDataFolder;
			Data.AppDataModsPath = modPakFolder;
			Data.AppDataCampaignsPath = gmCampaignsFolder;
			Data.AppDataProfilesPath = profileFolder;

			if (Directory.Exists(localAppDataFolder))
			{
				Directory.CreateDirectory(gameDataFolder);
				DivinityApp.Log($"Larian documents folder set to '{gameDataFolder}'.");

				if (!Directory.Exists(modPakFolder))
				{
					DivinityApp.Log($"No mods folder found at '{modPakFolder}'. Creating folder.");
					Directory.CreateDirectory(modPakFolder);
				}

#if DOS2
				if (!Directory.Exists(gmCampaignsFolder))
				{
					DivinityApp.Log($"No GM campaigns folder found at '{gmCampaignsFolder}'. Creating folder.");
					Directory.CreateDirectory(gmCampaignsFolder);
				}
#endif

				if (!Directory.Exists(profileFolder))
				{
					DivinityApp.Log($"No PlayerProfiles folder found at '{profileFolder}'. Creating folder.");
					Directory.CreateDirectory(profileFolder);
				}
			}
			else
			{
				DivinityApp.ShowAlert("Failed to find %LOCALAPPDATA% folder - This is weird", AlertType.Danger);
				DivinityApp.Log($"Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify) return a non-existent path?\nResult({Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify)})");
			}

			if (String.IsNullOrWhiteSpace(currentGameDataPath) || !Directory.Exists(currentGameDataPath))
			{
				var defaultPathways = _settingsService.AppSettings.DefaultPathways;
				var installPath = DivinityRegistryHelper.GetGameInstallPath(defaultPathways.Steam.RootFolderName,
					defaultPathways.GOG.Registry_32, defaultPathways.GOG.Registry_64, defaultPathways.Steam.AppID);

				if (!String.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
				{
					Data.InstallPath = installPath;
					if (!File.Exists(_settingsService.ManagerSettings.GameExecutablePath))
					{
						var exePath = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exePath = Path.Join(installPath, _settingsService.AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exePath = Path.Join(installPath, _settingsService.AppSettings.DefaultPathways.GOG.ExePath);
						}
						if (File.Exists(exePath))
						{
							_settingsService.ManagerSettings.GameExecutablePath = exePath.Replace("\\", "/");
							DivinityApp.Log($"Exe path set to '{exePath}'.");
						}
					}

					var gameDataPath = Path.Join(installPath, _settingsService.AppSettings.DefaultPathways.GameDataFolder).Replace("\\", "/");
					if (Directory.Exists(gameDataPath))
					{
						DivinityApp.Log($"Set game data path to '{gameDataPath}'.");
						_settingsService.ManagerSettings.GameDataPath = gameDataPath;
					}
					else
					{
						DivinityApp.Log($"Failed to find game data path at '{gameDataPath}'.");
					}
				}
			}
			else
			{
				var installPath = Path.GetFullPath(Path.Join(_settingsService.ManagerSettings.GameDataPath, @"..\..\"));
				Data.InstallPath = installPath;
				if (!File.Exists(_settingsService.ManagerSettings.GameExecutablePath))
				{
					var exePath = "";
					if (!DivinityRegistryHelper.IsGOG)
					{
						exePath = Path.Join(installPath, _settingsService.AppSettings.DefaultPathways.Steam.ExePath);
					}
					else
					{
						exePath = Path.Join(installPath, _settingsService.AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (File.Exists(exePath))
					{
						_settingsService.ManagerSettings.GameExecutablePath = exePath.Replace("\\", "/");
						DivinityApp.Log($"Exe path set to '{exePath}'.");
					}
				}
			}


			if (!Directory.Exists(_settingsService.ManagerSettings.GameDataPath) || !File.Exists(_settingsService.ManagerSettings.GameExecutablePath))
			{
				DivinityApp.Log("Failed to find game data path.");
				return false;
			}

			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error setting up game pathways: {ex}");
		}

		return false;
	}

	public PathwaysService(ISettingsService settingsService)
	{
		_settingsService = settingsService;
		Data = new();
	}
}
