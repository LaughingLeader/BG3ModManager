namespace ModManager.Models;

public class IgnoredModsData
{
	public List<string> IgnoreDependencies { get; set; } = new List<string>();
	public List<Dictionary<string, object>> Mods { get; set; } = new List<Dictionary<string, object>>();
	public List<string> IgnoreBuiltinPath { get; set; } = new List<string>();
}
