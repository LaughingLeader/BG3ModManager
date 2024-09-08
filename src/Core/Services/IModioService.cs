﻿using ModManager.Models.Mod;
using ModManager.Models.Updates;

namespace ModManager;
public interface IModioService
{
	string ApiKey { get; set; }
	bool IsInitialized { get; }
	bool LimitExceeded { get; }
	bool CanFetchData { get; }
	Uri ProfileAvatarUrl { get; }

	Task<UpdateResult> FetchModInfoAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
	//TODO
	Task<Dictionary<string, Modio.Models.Download>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
}
