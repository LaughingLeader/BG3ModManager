using ModManager.Models.Cache;
using ModManager.Models.Mod;

using Newtonsoft.Json;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ModManager.ModUpdater.Cache;

public class NexusModsCacheHandler : ReactiveObject, IExternalModCacheHandler<NexusModsCachedData>
{
	public ModSourceType SourceType => ModSourceType.NEXUSMODS;
	public string FileName => "nexusmodsdata.json";
	public JsonSerializerSettings SerializerSettings { get; }
	[Reactive] public bool IsEnabled { get; set; }
	public NexusModsCachedData CacheData { get; set; }

	public string APIKey { get; set; }
	public string AppName { get; set; }
	public string AppVersion { get; set; }

	public NexusModsCacheHandler(JsonSerializerSettings serializerSettings)
	{
		SerializerSettings = serializerSettings;
		CacheData = new NexusModsCachedData();
		IsEnabled = false;
	}

	public void OnCacheUpdated(NexusModsCachedData cachedData)
	{
		foreach (var entry in cachedData.Mods)
		{
			if (CacheData.Mods.TryGetValue(entry.Key, out var existing))
			{
				if (existing.UpdatedTimestamp < entry.Value.UpdatedTimestamp || !existing.IsUpdated)
				{
					CacheData.Mods[entry.Key] = entry.Value;
				}
			}
			else
			{
				CacheData.Mods[entry.Key] = entry.Value;
			}
		}
	}

	public async Task<bool> Update(IEnumerable<DivinityModData> mods, CancellationToken token)
	{
		var nexusModsService = AppServices.Get<INexusModsService>();
		if (nexusModsService.CanFetchData)
		{
			DivinityApp.Log("Checking for Nexus Mods updates.");
			var result = await nexusModsService.FetchModInfoAsync(mods, token);

			if (result.Success)
			{
				DivinityApp.Log($"Fetched NexusMods mod info for {result.UpdatedMods.Count} mod(s).");

				foreach (var mod in mods.Where(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START).Select(x => x.NexusModsData))
				{
					CacheData.Mods[mod.UUID] = mod;
				}

				return true;
			}
			else
			{
				DivinityApp.Log($"Failed to update NexusMods info:\n{result.FailureMessage}");
			}
		}
		else
		{
			DivinityApp.Log("NexusModsAPIKey not set, or daily/hourly limit reached. Skipping.");
		}
		return false;
	}
}
