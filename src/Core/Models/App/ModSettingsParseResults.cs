using ModManager.Models.Mod.Game;

namespace ModManager.Models.App;

public class ModSettingsParseResults
{
	public List<ModuleShortDesc> ActiveMods { get; set; }

	public ModSettingsParseResults()
	{
		ActiveMods = [];
	}

	public int CountActive(bool includeIgnoredMods = false)
	{
		var i = 0;
		foreach (var mod in ActiveMods)
		{
			if (includeIgnoredMods || !DivinityApp.IgnoredMods.Any(x => x.UUID == mod.UUID))
			{
				i++;
			}
		}
		return i;
	}
}
