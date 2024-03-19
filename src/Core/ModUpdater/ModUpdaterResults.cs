using DivinityModManager.Models.GitHub;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.NexusMods;

namespace DivinityModManager.ModUpdater;
public class ModUpdaterResults(Dictionary<string, GitHubLatestReleaseData> github, Dictionary<string, NexusModsModDownloadLink> nexusMods, Dictionary<string, DivinityModData> steamWorkshop)
{
	public Dictionary<string, GitHubLatestReleaseData> GitHub { get; } = github;
	public Dictionary<string, NexusModsModDownloadLink> NexusMods { get; } = nexusMods;
	public Dictionary<string, DivinityModData> SteamWorkshop { get; } = steamWorkshop;
}
