namespace ModManager.Models;

public class IgnoredModsData
{
	public List<string> IgnoreDependencies { get; set; } = [];
	public List<Dictionary<string, object>> Mods { get; set; } = [];
	public List<string> IgnoreBuiltinPath { get; set; } = [];
}
