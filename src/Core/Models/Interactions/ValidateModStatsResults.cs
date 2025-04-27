using LSLib.Stats;

using ModManager.Models.Mod;

namespace ModManager;

public record struct ValidateModStatsResults(List<ModData> Mods, List<StatLoadingError> Errors,
	Dictionary<string, string[]> FileText, TimeSpan TimeTaken);