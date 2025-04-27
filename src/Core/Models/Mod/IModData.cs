namespace ModManager.Models.Mod;

public interface IModData
{
	string? UUID { get; set; }
	string? Name { get; set; }
	string? Folder { get; set; }
	string? MD5 { get; set; }
	public ulong PublishHandle { get; set; }
	LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; }
}