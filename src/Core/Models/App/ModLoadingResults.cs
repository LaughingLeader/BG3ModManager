using DivinityModManager.Models.Mod;

namespace DivinityModManager.Models.App;

public class ModLoadingResults
{
	public string DirectoryPath { get; set; }
	public List<DivinityModData> Mods { get; set; }
	public List<DivinityModData> Duplicates { get; set; }

	public ModLoadingResults()
	{
		Mods = new List<DivinityModData>();
		Duplicates = new List<DivinityModData>();
	}
}
