namespace ModManager.Models;

public struct IgnoredModsEntry
{
	public string? UUID { get; set; }
	public string? Name { get; set; }
	public string? Description { get; set; }
	public string? Folder { get; set; }
	public string? Type { get; set; }
	public string? Author { get; set; }
	public string? Targets { get; set; }
	public ulong? Version { get; set; }
	public string? Tags { get; set; }
}

public class IgnoredModsData
{
	public List<string> IgnoreDependencies { get; set; } = [];
	public List<IgnoredModsEntry> Mods { get; set; } = [];
	public List<string> IgnoreBuiltinPath { get; set; } = [];
}
