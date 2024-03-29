namespace ModManager;

/// <summary>
/// Currently used to monitor when the game is launched, in order to prevent launching multiple instances of the game.
/// </summary>
public interface IGameUtilitiesService
{
	bool IsActive { get; }
	bool GameIsRunning { get; }
	TimeSpan ProcessCheckInterval { get; set; }

	void AddGameProcessName(string name);
	void AddGameProcessName(IEnumerable<string> names);
	void RemoveGameProcessName(string name);
	void RemoveGameProcessName(IEnumerable<string> names);

	void CheckForGameProcess();
}