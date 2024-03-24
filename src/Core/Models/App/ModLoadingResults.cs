using ModManager.Models.Mod;

namespace ModManager.Models.App;

public class ModLoadingResults
{
	public string DirectoryPath { get; set; }
	public List<DivinityModData> Mods { get; set; }
	public List<DivinityModData> Duplicates { get; set; }

	public ModLoadingResults()
	{
		Mods = [];
		Duplicates = [];
	}
}
