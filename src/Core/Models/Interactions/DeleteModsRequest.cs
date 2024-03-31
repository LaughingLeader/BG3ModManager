using ModManager.Models.Mod;

namespace ModManager;

public record DeleteModsRequest(IEnumerable<IModEntry> TargetMods, bool IsDeletingDuplicates = false);