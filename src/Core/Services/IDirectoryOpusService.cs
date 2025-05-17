namespace ModManager.Services;
public interface IDirectoryOpusService
{
	bool IsEnabled { get; }

	/// <summary>
	/// Get the executable path from the registry.
	/// </summary>
	/// <returns></returns>
	string? GetExecutablePath();

	/// <summary>
	/// Tries to open the file in Directory Opus.
	/// </summary>
	/// <param name="filePath">The path to the file/directory to open.</param>
	/// <param name="focus">Focus the DirectoryOpus lister window.</param>
	/// <param name="exePath">The path to dopusrt.exe. Fetched from the registry if not set.</param>
	void OpenInDirectoryOpus(string? filePath, bool focus = true, string? exePath = null);
}
