using AutoUpdateViaGitHubRelease;

using ModManager.Models.App;

namespace ModManager.Services;
public class AppUpdaterService(IFileSystemService fs, IGitHubService githubService, IEnvironmentService environment) : IAppUpdaterService
{
	private readonly IFileSystemService _fs = fs;
	private readonly IGitHubService _github = githubService;
	private readonly IEnvironmentService _environment = environment;

	public string AppTitle => _environment.AppProductName;
	public Version CurrentVersion => _environment.AppVersion;
	public string? GitHubUser { get; set; }
	public string? GitHubRepo { get; set; }
	public string? TempDirectory { get; set; }
	public string? TempFileName { get; set; }

	public void Configure(string gitHubUser, string gitHubRepo, string tempFileName)
	{
		GitHubUser = gitHubUser;
		GitHubRepo = gitHubRepo;
		TempFileName = tempFileName;
	}

	public async Task<bool> DownloadAndInstallUpdateAsync()
	{
		var tempDir = TempDirectory;
		if (string.IsNullOrEmpty(tempDir))
		{
			tempDir = _fs.Path.Join(_fs.Path.GetTempPath(), AppTitle);
		}
		_fs.EnsureDirectoryExists(tempDir);
		var tempFile = _fs.Path.Join(tempDir, TempFileName);

		var result = await UpdateTools.CheckDownloadNewVersionAsync(GitHubUser, GitHubRepo, CurrentVersion, tempFile);
		if(result)
		{
			var installerResult = await UpdateTools.DownloadExtractInstallerToAsync(tempDir);
			if(!string.IsNullOrEmpty(installerResult))
			{
				var installer = _fs.Path.Join(tempDir, installerResult);
				var destinationDir = _fs.Path.GetDirectoryName(_environment.AppDirectory);
				UpdateTools.StartInstall(installer, tempFile, destinationDir);
				Environment.Exit(0);
				return true;
			}
		}
		return false;
	}

	public async Task<AppUpdateResult> CheckForUpdatesAsync()
	{
		if(!string.IsNullOrEmpty(GitHubUser) && !string.IsNullOrEmpty(GitHubRepo))
		{
			var latestRelease = await _github.GetLatestReleaseAsync(GitHubUser, GitHubRepo);
			if (latestRelease != null)
			{
				if (Version.TryParse(latestRelease.Version, out var latestVersion))
				{
					if (latestVersion > CurrentVersion)
					{
						return new AppUpdateResult(true, latestVersion, latestRelease.Date, latestRelease.BrowserDownloadLink);
					}
				}
			}
		}

		return new AppUpdateResult(false, null, DateTimeOffset.Now, null);
	}
}
