using ModManager.Models.Mod;

namespace ModManager;
public static class ModExtensions
{
	public static IEnumerable<IModEntry> ToModInterface(this IEnumerable<ModData> mods) => mods.Select(x => new ModEntry(x));
	public static IModEntry ToModInterface(this ModData mod) => new ModEntry(mod);
}
