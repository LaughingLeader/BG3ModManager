﻿using DivinityModManager.Models;
using DivinityModManager.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.AppServices;
public class PathwaysService
{
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
		if (!String.IsNullOrEmpty(Services.Settings.ManagerSettings.DocumentsFolderPathOverride))
		{
			appDataFolder = Services.Settings.ManagerSettings.DocumentsFolderPathOverride;
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
		var settings = Services.Settings;
		try
		{
			string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

			if (String.IsNullOrWhiteSpace(settings.AppSettings.DefaultPathways.DocumentsGameFolder))
			{
				settings.AppSettings.DefaultPathways.DocumentsGameFolder = "Larian Studios\\Baldur's Gate 3";
			}

			string gameDataFolder = Path.Join(localAppDataFolder, settings.AppSettings.DefaultPathways.DocumentsGameFolder);

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
					gameDataFolder = Path.Join(localAppDataFolder, settings.AppSettings.DefaultPathways.DocumentsGameFolder);
				}
			}

			string modPakFolder = Path.Join(gameDataFolder, "Mods");
			string gmCampaignsFolder = Path.Join(gameDataFolder, "GMCampaigns");
			string profileFolder = Path.Join(gameDataFolder, "PlayerProfiles");

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
				string installPath = DivinityRegistryHelper.GetGameInstallPath(settings.AppSettings.DefaultPathways.Steam.RootFolderName,
					settings.AppSettings.DefaultPathways.GOG.Registry_32, settings.AppSettings.DefaultPathways.GOG.Registry_64);

				if (!String.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
				{
					Data.InstallPath = installPath;
					if (!File.Exists(settings.ManagerSettings.GameExecutablePath))
					{
						string exePath = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exePath = Path.Join(installPath, settings.AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exePath = Path.Join(installPath, settings.AppSettings.DefaultPathways.GOG.ExePath);
						}
						if (File.Exists(exePath))
						{
							settings.ManagerSettings.GameExecutablePath = exePath.Replace("\\", "/");
							DivinityApp.Log($"Exe path set to '{exePath}'.");
						}
					}

					string gameDataPath = Path.Join(installPath, settings.AppSettings.DefaultPathways.GameDataFolder).Replace("\\", "/");
					if (Directory.Exists(gameDataPath))
					{
						DivinityApp.Log($"Set game data path to '{gameDataPath}'.");
						settings.ManagerSettings.GameDataPath = gameDataPath;
					}
					else
					{
						DivinityApp.Log($"Failed to find game data path at '{gameDataPath}'.");
					}
				}
			}
			else
			{
				string installPath = Path.GetFullPath(Path.Join(settings.ManagerSettings.GameDataPath, @"..\..\"));
				Data.InstallPath = installPath;
				if (!File.Exists(settings.ManagerSettings.GameExecutablePath))
				{
					string exePath = "";
					if (!DivinityRegistryHelper.IsGOG)
					{
						exePath = Path.Join(installPath, settings.AppSettings.DefaultPathways.Steam.ExePath);
					}
					else
					{
						exePath = Path.Join(installPath, settings.AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (File.Exists(exePath))
					{
						settings.ManagerSettings.GameExecutablePath = exePath.Replace("\\", "/");
						DivinityApp.Log($"Exe path set to '{exePath}'.");
					}
				}
			}


			if (!Directory.Exists(settings.ManagerSettings.GameDataPath) || !File.Exists(settings.ManagerSettings.GameExecutablePath))
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

	public PathwaysService()
	{
		Data = new();
	}
}
