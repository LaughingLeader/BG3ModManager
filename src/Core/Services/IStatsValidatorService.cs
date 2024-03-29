using ModManager.Models.Mod;

namespace ModManager;
public interface IStatsValidatorService
{
	string? GameDataPath { get; }
	void Initialize(string gameDataPath);
	Task<ValidateModStatsResults> ValidateModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
}
