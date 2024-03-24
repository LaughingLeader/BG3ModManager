using ModManager.Models.GitHub;
using ModManager.Models.Mod;

namespace ModManager;

public interface IGitHubService
{
	Task<GitHubLatestReleaseData> GetLatestReleaseAsync(string owner, string repo);
	Task<Dictionary<string, GitHubLatestReleaseData>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
}
