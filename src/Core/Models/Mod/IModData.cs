using ModManager.Models.Interfaces;

namespace ModManager.Models.Mod;

public interface IModData : INamedEntry
{
	string UUID { get; }
	string? Folder { get; set; }
	string? MD5 { get; set; }
	public ulong PublishHandle { get; set; }
	LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; }
}