using ModManager.Models.Mod;

namespace ModManager.Models.Conflicts;

public class DivinityConflictEntryData : ReactiveObject
{
	[Reactive] public string? Target { get; set; }
	[Reactive] public string? Name { get; set; }

	public List<DivinityConflictModData> ConflictModDataList { get; set; } = [];
}

public class DivinityConflictModData(ModData mod, string val = "") : ReactiveObject
{
	public ModData Mod => mod;

	public string? Value { get; set; } = val;
}
