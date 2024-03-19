using DivinityModManager.Models.Mod;

namespace DivinityModManager.Models.App;

public struct ImportOperationError
{
	public Exception Exception { get; set; }
	public string File { get; set; }
}

public class ImportOperationResults
{
	public bool Success => Mods.Count >= TotalPaks;
	public int TotalFiles { get; set; }
	public int TotalPaks { get; set; }
	public List<DivinityModData> Mods { get; set; } = new List<DivinityModData>();
	public List<DivinityLoadOrder> Orders { get; set; } = new List<DivinityLoadOrder>();
	public List<ImportOperationError> Errors { get; set; } = new List<ImportOperationError>();

	public void AddError(string path, Exception ex) => Errors.Add(new ImportOperationError { Exception = ex, File = path });
}
