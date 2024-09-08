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
	Task<bool> UpdateInfoAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
	Task<bool> LoadCacheAsync(IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken token);
	Task<bool> SaveCacheAsync(IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken token);

	Task<ModUpdaterResults> FetchUpdatesAsync(ModManagerSettings settings, IEnumerable<DivinityModData> mods, CancellationToken token);
	Task<Dictionary<string, GitHubLatestReleaseData>> GetGitHubUpdatesAsync(IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken token);
	Task<Dictionary<string, NexusModsModDownloadLink>> GetNexusModsUpdatesAsync(IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken token);
	Task<Dictionary<string, Modio.Models.Download>> GetModioUpdatesAsync(ModManagerSettings settings, IEnumerable<DivinityModData> mods, string currentAppVersion, CancellationToken token);

	bool DeleteCache();
}