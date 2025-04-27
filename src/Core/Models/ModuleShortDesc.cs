using ModManager.Models.Mod;

using System.Runtime.Serialization;

namespace ModManager.Models;

[DataContract]
public class ModuleShortDesc : ReactiveObject, IModData
{
	[DataMember, Reactive] public string? UUID { get; set; }
	[DataMember, Reactive] public string? Name { get; set; }
	[DataMember, Reactive] public string? Folder { get; set; }
	[DataMember, Reactive] public string? MD5 { get; set; }
	[DataMember, Reactive] public ulong PublishHandle { get; set; }
	[DataMember, Reactive] public LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; set; }

	public override string ToString() => $"[ModuleShortDesc] Name({Name}) UUID({UUID}) Version({Version?.Version})";

	public static ModuleShortDesc FromModData(ModData m)
	{
		return new ModuleShortDesc
		{
			Folder = m.Folder,
			Name = m.Name,
			UUID = m.UUID,
			MD5 = m.MD5,
			PublishHandle = m.PublishHandle,
			Version = m.Version,
			LastModified = m.LastModified
		};
	}

	public ModuleShortDesc()
	{
		UUID = "";
		Name = "";
		Folder = "";
		MD5 = "";
		PublishHandle = 0ul;
		Version = new();
	}
}
