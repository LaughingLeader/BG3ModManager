using ModManager.Models.Mod;

namespace ModManager;
public static class ModExtensions
{
	public static IEnumerable<IModEntry> ToModInterface(this IEnumerable<DivinityModData> mods) => mods.Select(x => new ModEntry(x));
	public static IModEntry ToModInterface(this DivinityModData mod) => new ModEntry(mod);
}
