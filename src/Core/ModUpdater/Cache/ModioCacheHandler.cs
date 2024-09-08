using ModManager.Models.Cache;
using ModManager.Models.Mod;
using ModManager.Util;
using Modio;
using Modio.Filters;

namespace ModManager.ModUpdater.Cache;

public class ModioCacheHandler : ReactiveObject, IExternalModCacheHandler<ModioCachedData>
{
	public ModSourceType SourceType => ModSourceType.MODIO;
	public string FileName => "modiodata.json";
	public JsonSerializerOptions SerializerSettings { get; }
	public ModioCachedData CacheData { get; set; }
	[Reactive] public bool IsEnabled { get; set; }

	public ModioCacheHandler(JsonSerializerOptions serializerSettings)
	{
		SerializerSettings = serializerSettings;
		CacheData = new ModioCachedData();
		IsEnabled = false;
	}

	public void OnCacheUpdated(ModioCachedData cachedData)
	{
		foreach (var entry in cachedData.Mods)
		{
			if (CacheData.Mods.TryGetValue(entry.Key, out var existing))
			{
				if (existing.DateUpdated < entry.Value.DateUpdated)
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
		var apiService = Locator.Current.GetService<IModioService>();
		if (apiService?.CanFetchData == true)
		{
			DivinityApp.Log("Checking for mod.io updates.");
			var result = await apiService.FetchModInfoAsync(mods, token);

			if (result.Success)
			{
				DivinityApp.Log($"Fetched mod.io info for {result.UpdatedMods.Count} mod(s).");

				foreach (var mod in mods.Where(x => x.PublishHandle > 0 && x.ModioData.IsEnabled))
				{
					CacheData.Mods[mod.UUID!] = mod.ModioData.Data!;
				}

				return true;
			}
			else
			{
				DivinityApp.Log($"Failed to update mod.io info:\n{result.FailureMessage}");
			}
		}
		else
		{
			DivinityApp.Log("mod.io API web key not set, or daily/hourly limit reached. Skipping.");
		}
		return false;
	}
}
