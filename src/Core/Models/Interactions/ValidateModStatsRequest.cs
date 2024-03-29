using ModManager.Models.Mod;

namespace ModManager;

public record struct ValidateModStatsRequest(List<DivinityModData> Mods, CancellationToken Token);