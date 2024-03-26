using ModManager.Models.Mod;

namespace ModManager;

public record DeleteModsRequest(List<DivinityModData> TargetMods, 
	bool IsDeletingDuplicates = false, IEnumerable<DivinityModData>? LoadedMods = null);