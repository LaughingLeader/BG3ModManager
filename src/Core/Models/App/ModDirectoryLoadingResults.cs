using ModManager.Models.Mod;

namespace ModManager.Models.App;

public class ModDirectoryLoadingResults
{
	public string? DirectoryPath { get; }
	public Dictionary<string, DivinityModData> Mods { get; init; }
	public List<DivinityModData> Duplicates { get; init; }

	public ModDirectoryLoadingResults(string path)
	{
		DirectoryPath = path;
		Mods = [];
		Duplicates = [];
	}
}
