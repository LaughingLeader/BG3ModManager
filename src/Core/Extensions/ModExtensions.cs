using ModManager.Models;
using ModManager.Models.Mod;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class ModExtensions
{
	public static IEnumerable<IModEntry> ToModInterface(this IEnumerable<DivinityModData> mods) => mods.Select(x => new ModEntry(x));
	public static IModEntry ToModInterface(this DivinityModData mod) => new ModEntry(mod);
}
