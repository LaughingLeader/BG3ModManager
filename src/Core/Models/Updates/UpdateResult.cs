using ModManager.Models.Mod;

namespace ModManager.Models.Updates;

public class UpdateResult
{
	public List<DivinityModData> UpdatedMods { get; set; }
	public string? FailureMessage { get; set; }
	public bool Success { get; set; }

	public UpdateResult()
	{
		UpdatedMods = [];
		Success = true;
	}
}
