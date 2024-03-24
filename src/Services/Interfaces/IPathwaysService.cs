using ModManager.Models;

namespace ModManager;

public interface IPathwaysService
{
	DivinityPathwayData Data { get; }

	string GetLarianStudiosAppDataFolder();
	bool SetGamePathways(string currentGameDataPath, string gameDataFolderOverride = "");
}