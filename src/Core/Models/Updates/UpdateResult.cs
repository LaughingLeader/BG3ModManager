using DivinityModManager.Models.Mod;

namespace DivinityModManager.Models.Updates;

public class UpdateResult
{
	public List<DivinityModData> UpdatedMods { get; set; }
	public string FailureMessage { get; set; }
	public bool Success { get; set; }

	public UpdateResult()
	{
		UpdatedMods = new List<DivinityModData>();
		Success = true;
	}
}
