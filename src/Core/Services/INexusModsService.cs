using DynamicData.Binding;

using ModManager.Models.Mod;
using ModManager.Models.NexusMods;
using ModManager.Models.Updates;

namespace ModManager;

public interface INexusModsService
{
	string? ApiKey { get; set; }
	bool IsInitialized { get; }
	bool LimitExceeded { get; }
	bool CanFetchData { get; }
	bool IsPremium { get; }
	Uri? ProfileAvatarUrl { get; }

	double DownloadProgressValue { get; }
	string? DownloadProgressText { get; }
	bool CanCancel { get; }

	NexusModsObservableApiLimits ApiLimits { get; }

	IObservable<NexusModsObservableApiLimits?> WhenLimitsChange { get; }

	ObservableCollectionExtended<string> DownloadResults { get; }

	Task<Dictionary<string, NexusModsModDownloadLink>> GetLatestDownloadsForModsAsync(IEnumerable<ModData> mods, CancellationToken token);
	Task<UpdateResult> FetchModInfoAsync(IEnumerable<ModData> mods, CancellationToken token);
	void ProcessNXMLinkBackground(string url);
	void CancelDownloads();
}