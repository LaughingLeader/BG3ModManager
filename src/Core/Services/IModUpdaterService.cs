using ModManager.Models.GitHub;
using ModManager.Models.Mod;
using ModManager.Models.NexusMods;
using ModManager.Models.Settings;
using ModManager.ModUpdater;
using ModManager.ModUpdater.Cache;

namespace ModManager;

public interface IModUpdaterService
{
	bool IsRefreshing { get; set; }
	NexusModsCacheHandler NexusMods { get; }
	ModioCacheHandler Modio { get; }
	GitHubModsCacheHandler GitHub { get; }
	Task<bool> UpdateInfoAsync(IEnumerable<ModData> mods, CancellationToken token);
	Task<bool> LoadCacheAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token);
	Task<bool> SaveCacheAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token);

	Task<ModUpdaterResults> FetchUpdatesAsync(ModManagerSettings settings, IEnumerable<ModData> mods, CancellationToken token);
	Task<Dictionary<string, GitHubLatestReleaseData>> GetGitHubUpdatesAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token);
	Task<Dictionary<string, NexusModsModDownloadLink>> GetNexusModsUpdatesAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token);
	Task<Dictionary<string, Modio.Models.Download>> GetModioUpdatesAsync(ModManagerSettings settings, IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token);

	bool DeleteCache();
}