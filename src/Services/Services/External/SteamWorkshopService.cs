using ModManager.Models.Mod;

namespace ModManager.Services;
public class SteamWorkshopService : ISteamWorkshopService
{
	public async Task<List<DivinityModData>> CheckForWorkshopModUpdatesAsync(CancellationToken token)
	{
		var mods = new List<DivinityModData>();



		return mods;
	}
}
