using ModManager.Models.App;

namespace ModManager.Services;
public interface IAppUpdaterService
{
	string AppTitle { get; }
	Version CurrentVersion { get; }
	string? GitHubUser { get; }
	string? GitHubRepo { get; }
	string? TempDirectory { get; }

	void Configure(string gitHubUser, string gitHubRepo, string tempFileName);

	Task<bool> DownloadAndInstallUpdateAsync();
	Task<AppUpdateResult> CheckForUpdatesAsync();
}
