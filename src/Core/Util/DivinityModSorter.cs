using DivinityModManager.Models.Mod;

namespace DivinityModManager.Util;

public static class DivinityModSorter
{
	public static IEnumerable<DivinityModData> SortAlphabetical(IEnumerable<DivinityModData> mods)
	{
		return mods.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase);
	}
}
