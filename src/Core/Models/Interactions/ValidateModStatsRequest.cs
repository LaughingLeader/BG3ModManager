using ModManager.Models.Mod;

namespace ModManager;

public record struct ValidateModStatsRequest(List<ModData> Mods, CancellationToken Token);