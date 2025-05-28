using ModManager.Models.Interfaces;

namespace ModManager.Models.Mod;

public interface IModuleShortDesc : INamedEntry
{
	string UUID { get; }
	string? Folder { get; set; }
	string? MD5 { get; set; }
	ulong PublishHandle { get; set; }
	LarianVersion Version { get; set; }
	DateTimeOffset? LastModified { get; }
}