using ModManager.Models;

namespace ModManager;

public interface IPathwaysService
{
	PathwayData Data { get; }

	string GetLarianStudiosAppDataFolder();
	bool SetGamePathways(string currentGameDataPath, string gameDataFolderOverride = "");
}