namespace ModManager.Models.App;
public class ModsLoadingResults
{
	public ModDirectoryLoadingResults DataDirectoryMods { get; }
	public ModDirectoryLoadingResults UserDirectoryMods { get; }

	public ModsLoadingResults(string dataDirectory, string userModsDirectory)
	{
		DataDirectoryMods = new(dataDirectory);
		UserDirectoryMods = new(userModsDirectory);
	}

	public ModsLoadingResults(ModDirectoryLoadingResults dataDirectoryResults, ModDirectoryLoadingResults userModsResults)
	{
		DataDirectoryMods = dataDirectoryResults;
		UserDirectoryMods = userModsResults;
	}
}
