using Modio.Models;

using ModManager.Models.Mod;
using ModManager.Models.Updates;

namespace ModManager;
public interface IModioService
{
	string ApiKey { get; set; }
	bool IsInitialized { get; }
	bool LimitExceeded { get; }
	bool CanFetchData { get; }

	Task<UpdateResult> FetchModInfoAsync(IEnumerable<ModData> mods, CancellationToken token);
	Task<Dictionary<string, Download>> GetLatestDownloadsForModsAsync(IEnumerable<ModData> mods, CancellationToken token);
	Task<List<string>> DownloadFilesForModsAsync(IEnumerable<ModData> mods, CancellationToken token);
}
