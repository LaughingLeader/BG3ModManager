using ModManager.Models.Mod;

namespace ModManager.Util;

public static class DivinityModSorter
{
	public static IEnumerable<ModData> SortAlphabetical(IEnumerable<ModData> mods)
	{
		return mods.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase);
	}
}
