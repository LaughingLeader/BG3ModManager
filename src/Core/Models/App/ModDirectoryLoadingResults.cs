using ModManager.Models.Mod;

namespace ModManager.Models.App;

public class ModDirectoryLoadingResults
{
	public string? DirectoryPath { get; }
	public Dictionary<string, ModData> Mods { get; init; }
	public List<ModData> Duplicates { get; init; }

	public ModDirectoryLoadingResults(string path)
	{
		DirectoryPath = path;
		Mods = [];
		Duplicates = [];
	}
}
