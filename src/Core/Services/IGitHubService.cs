using ModManager.Models.GitHub;
using ModManager.Models.Mod;

using Octokit;

namespace ModManager;

public interface IGitHubService
{
	Task<GitHubLatestReleaseData> GetLatestReleaseAsync(string owner, string repo);
	Task<Release?> GetLatestReleaseRawAsync(string owner, string repo);

	Task<Dictionary<string, GitHubLatestReleaseData>> GetLatestDownloadsForModsAsync(IEnumerable<ModData> mods, CancellationToken token);
}
