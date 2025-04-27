using ModManager.Models.Mod;

namespace ModManager.Services;
public class SteamWorkshopService : ISteamWorkshopService
{
	public async Task<List<ModData>> CheckForWorkshopModUpdatesAsync(CancellationToken token)
	{
		var mods = new List<ModData>();



		return mods;
	}
}
