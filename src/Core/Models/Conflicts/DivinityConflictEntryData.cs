using ModManager.Models.Mod;

using ReactiveUI;

namespace ModManager.Models.Conflicts;

public class DivinityConflictEntryData : ReactiveObject
{
	private string target;

	public string Target
	{
		get => target;
		set { this.RaiseAndSetIfChanged(ref target, value); }
	}

	private string name;

	public string Name
	{
		get => name;
		set { this.RaiseAndSetIfChanged(ref name, value); }
	}

	public List<DivinityConflictModData> ConflictModDataList { get; set; } = new List<DivinityConflictModData>();
}

public class DivinityConflictModData : ReactiveObject
{
	private readonly DivinityModData modData;
	public DivinityModData Mod => modData;

	public string Value { get; set; }

	public DivinityConflictModData(DivinityModData mod, string val = "")
	{
		modData = mod;
		Value = val;
	}
}
