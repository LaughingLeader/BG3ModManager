using Gameloop.Vdf;
using Gameloop.Vdf.Linq;

using Microsoft.Win32;

using System.IO;

namespace ModManager.Util;

public static class DivinityRegistryHelper
{
	const string REG_Steam_32 = @"SOFTWARE\Valve\Steam";
	const string REG_Steam_64 = @"SOFTWARE\Wow6432Node\Valve\Steam";
	const string REG_GOG_32 = @"SOFTWARE\GOG.com\Games";
	const string REG_GOG_64 = @"SOFTWARE\Wow6432Node\GOG.com\Games";

	const string REG_NXM_PROTOCOL_COMMAND = @"nxm\shell\open\command";

	const string PATH_Steam_WorkshopFolder = @"steamapps/workshop";
	const string PATH_Steam_LibraryFile = @"steamapps/libraryfolders.vdf";

	private static string lastSteamInstallPath = "";
	private static string LastSteamInstallPath
	{
		get
		{
			if (lastSteamInstallPath == "" || !Directory.Exists(lastSteamInstallPath))
			{
				lastSteamInstallPath = GetSteamInstallPath();
			}
			return lastSteamInstallPath;
		}
	}

	private static string lastGamePath = "";
	private static bool isGOG = false;
	public static bool IsGOG => isGOG;

	private static object GetKey(RegistryKey reg, string subKey, string keyValue)
	{
		try
		{
			RegistryKey key = reg.OpenSubKey(subKey);
			if (key != null)
			{
				return key.GetValue(keyValue);
			}
		}
		catch (Exception e)
		{
			DivinityApp.Log($"Error reading registry subKey ({subKey}): {e}");
		}
		return null;
	}

	public static string GetTruePath(string path)
	{
		try
		{
			var driveType = FileUtils.GetPathDriveType(path);
			if (driveType == System.IO.DriveType.Fixed)
			{
				if (JunctionPoint.Exists(path))
				{
					string realPath = JunctionPoint.GetTarget(path);
					if (!String.IsNullOrEmpty(realPath))
					{
						return realPath;
					}
				}
			}
			else
			{
				DivinityApp.Log($"Skipping junction check for path '{path}'. Drive type is '{driveType}'.");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error checking junction point '{path}': {ex}");
		}
		return path;
	}

	public static string GetSteamInstallPath()
	{
		RegistryKey reg = Registry.LocalMachine;
		object installPath = GetKey(reg, REG_Steam_64, "InstallPath");
		if (installPath == null)
		{
			installPath = GetKey(reg, REG_Steam_32, "InstallPath");
		}
		if (installPath != null)
		{
			return (string)installPath;
		}
		return "";
	}

	public static string GetSteamWorkshopPath()
	{
		if (LastSteamInstallPath != "")
		{
			string workshopFolder = Path.Join(LastSteamInstallPath, PATH_Steam_WorkshopFolder);
			DivinityApp.Log($"Looking for workshop folder at '{workshopFolder}'.");
			if (Directory.Exists(workshopFolder))
			{
				return workshopFolder;
			}
		}
		return "";
	}

	public static string GetWorkshopPath(string appid)
	{
		if (LastSteamInstallPath != "")
		{
			string steamWorkshopPath = GetSteamWorkshopPath();
			if (!String.IsNullOrEmpty(steamWorkshopPath))
			{
				string workshopFolder = Path.Join(steamWorkshopPath, "content", appid);
				DivinityApp.Log($"Looking for game workshop folder at '{workshopFolder}'.");
				if (Directory.Exists(workshopFolder))
				{
					return workshopFolder;
				}
			}
		}
		return "";
	}

	public static string GetGOGInstallPath(string gogRegKey32, string gogRegKey64)
	{
		RegistryKey reg = Registry.LocalMachine;
		object installPath = GetKey(reg, gogRegKey32, "path");
		if (installPath == null)
		{
			installPath = GetKey(reg, gogRegKey64, "path");
		}
		if (installPath != null)
		{
			return (string)installPath;
		}
		return "";
	}

	public static string GetGameInstallPath(string steamGameInstallPath, string gogRegKey32, string gogRegKey64, string steamAppId)
	{
		try
		{
			if (LastSteamInstallPath != "")
			{
				if (!String.IsNullOrEmpty(lastGamePath) && Directory.Exists(lastGamePath))
				{
					return lastGamePath;
				}

				var appManifest = Path.Join(LastSteamInstallPath, "steamapps", $"appmanifest_{steamAppId}.acf");
				if (File.Exists(appManifest))
				{
					var manifestData = VdfConvert.Deserialize(File.ReadAllText(appManifest));
					if (manifestData != null)
					{
						foreach (var prop in manifestData.Value.Children().OfType<VProperty>())
						{
							if (prop.Key == "installdir")
							{
								var installDir = prop.Value?.Value<string>();
								if (!String.IsNullOrEmpty(installDir))
								{
									steamGameInstallPath = installDir;
									DivinityApp.Log($"Using appmanifest installDir '{installDir}'");
								}
								break;
							}
						}
					}
				}

				string folder = Path.Join(LastSteamInstallPath, "steamapps", "common", steamGameInstallPath);
				DivinityApp.Log($"Looking for game at '{folder}'.");
				if (Directory.Exists(folder))
				{
					DivinityApp.Log($"Found game at '{folder}'.");
					lastGamePath = folder;
					isGOG = false;
					return lastGamePath;
				}
				else
				{
					string libraryFile = Path.Join(LastSteamInstallPath, PATH_Steam_LibraryFile);
					DivinityApp.Log($"Game not found. Looking for Steam libraries in file '{libraryFile}'.");
					if (File.Exists(libraryFile))
					{
						List<string> libraryFolders = new();
						try
						{
							var libraryData = VdfConvert.Deserialize(File.ReadAllText(libraryFile));
							foreach (VProperty token in libraryData.Value.Children())
							{
								if (token.Key != "TimeNextStatsReport" && token.Key != "ContentStatsID")
								{
									var path = token.Value.Children().Cast<VProperty>().FirstOrDefault(x => x.Key == "path");
									if (path != null && path.Value is VValue innerValue)
									{
										var p = innerValue.Value<string>();
										if (!String.IsNullOrEmpty(p) && Directory.Exists(p))
										{
											DivinityApp.Log($"Found steam library folder at '{p}'.");
											libraryFolders.Add(p);
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error parsing steam library file at '{libraryFile}': {ex}");
						}

						foreach (var folderPath in libraryFolders)
						{
							string checkFolder = Path.Join(folderPath, "steamapps", "common", steamGameInstallPath);
							if (!String.IsNullOrEmpty(checkFolder) && Directory.Exists(checkFolder))
							{
								DivinityApp.Log($"Found game at '{checkFolder}'.");
								lastGamePath = checkFolder;
								isGOG = false;
								return lastGamePath;
							}
						}
					}
					else
					{
						DivinityApp.Log($"Steam library not found at '{libraryFile}'");
					}
				}
			}

			string gogGamePath = GetGOGInstallPath(gogRegKey32, gogRegKey64);
			if (!String.IsNullOrEmpty(gogGamePath) && Directory.Exists(gogGamePath))
			{
				isGOG = true;
				lastGamePath = gogGamePath;
				DivinityApp.Log($"Found game (GoG) install at '{lastGamePath}'.");
				return lastGamePath;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"[*ERROR*] Error finding game path: {ex}");
		}

		return "";
	}

	public static bool IsAssociatedWithNXMProtocol(string appExePath)
	{
		//Get the "(Default)" key value
		var shellCommand = GetKey(Registry.ClassesRoot, REG_NXM_PROTOCOL_COMMAND, String.Empty)?.ToString();
		DivinityApp.Log($"{REG_NXM_PROTOCOL_COMMAND}: {shellCommand}");
		if (!String.IsNullOrEmpty(shellCommand))
		{
			return shellCommand.IndexOf(appExePath, StringComparison.OrdinalIgnoreCase) > -1;
		}
		return false;
	}

	public static bool AssociateWithNXMProtocol(string appExePath)
	{
		try
		{
			var reg = Registry.ClassesRoot;
			var shellCommand = GetKey(Registry.ClassesRoot, REG_NXM_PROTOCOL_COMMAND, String.Empty)?.ToString();
			if (String.IsNullOrEmpty(shellCommand))
			{
				var baseKey = reg.CreateSubKey("nxm", true);
				baseKey.SetValue(String.Empty, "URL:NXM Protocol", RegistryValueKind.String);
				baseKey.SetValue("URL Protocol", "", RegistryValueKind.String);
				var shellCommandKey = baseKey.CreateSubKey("shell").CreateSubKey("open").CreateSubKey("command");
				shellCommandKey.SetValue(String.Empty, $"\"{appExePath}\" \"%1\"", RegistryValueKind.String);
				reg.Close();
			}
			else if (shellCommand.IndexOf(appExePath, StringComparison.OrdinalIgnoreCase) == -1)
			{
				var key = reg.OpenSubKey(REG_NXM_PROTOCOL_COMMAND, true);
				key.SetValue(String.Empty, $"\"{appExePath}\" \"%1\"", RegistryValueKind.String);
			}
			reg.Close();
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error updating nxm protocol:\n{ex}");
		}
		return false;
	}
}
