using Microsoft.Win32;

using System.Runtime.InteropServices;

namespace ModManager.Services;

/// <inheritdoc />
public class RegistryService : IRegistryService
{
	private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

	/// <inheritdoc />
	public string? GetApplicationInstallPath(string displayName)
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return null;
		}

		using var rk = Registry.LocalMachine.OpenSubKey(UninstallKey);
		if (rk != null)
		{
			foreach (var skName in rk.GetSubKeyNames())
			{
				using var sk = rk.OpenSubKey(skName);
				if (sk != null && sk.GetValue("DisplayName")?.ToString() == displayName)
				{
					var exeDirectory = sk.GetValue("InstallLocation")?.ToString();
					if(!string.IsNullOrEmpty(exeDirectory))
					{
						return exeDirectory;
					}
				}
			}
		}
		return null;
	}
}
