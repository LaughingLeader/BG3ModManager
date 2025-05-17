namespace ModManager.Services;

public interface IRegistryService
{
	/// <summary>
	/// Get a program's installed directory via the Registry.
	/// </summary>
	/// <param name="displayName"></param>
	/// <returns></returns>
	string? GetApplicationInstallPath(string displayName);
}
