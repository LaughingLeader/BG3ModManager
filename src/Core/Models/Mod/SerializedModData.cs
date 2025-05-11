using ModManager.Models.Mod.Game;

using System.Runtime.Serialization;

namespace ModManager.Models.Mod;

[DataContract]
public class SerializedModData : IModData
{
	[DataMember] public string UUID { get; set; }
	[DataMember] public int Index { get; set; }
	[DataMember] public string? FileName { get; set; }
	[DataMember] public string? Folder { get; set; }
	[DataMember] public string? Name { get; set; }
	[DataMember] public string? Description { get; set; }
	[DataMember] public string? Author { get; set; }
	[DataMember] public ulong PublishHandle { get; set; }

	[DataMember] public LarianVersion Version { get; set; }

	[DataMember] public string? Type { get; set; }

	[DataMember] public ModScriptExtenderConfig? ScriptExtenderData { get; set; }
	[DataMember] public List<ModuleShortDesc> Dependencies { get; set; }

	[DataMember] public string? MD5 { get; set; }

	public DateTimeOffset? LastModified { get; set; }

	public SerializedModData(string uuid)
	{
		UUID = uuid;
		Dependencies = [];
		Version = new LarianVersion();
	}

	[JsonConstructor]
	public SerializedModData() : this(string.Empty)
	{
		
	}

	public static SerializedModData FromMod(ModData mod)
	{
		var result = new SerializedModData(mod.UUID)
		{
			Author = mod.AuthorDisplayName,
			Description = mod.Description,
			FileName = mod.FileName,
			Folder = mod.Folder,
			Name = mod.Name,
			Version = mod.Version,
			Type = mod.ModType,
			Index = mod.Index,
			ScriptExtenderData = mod.ScriptExtenderData,
			UUID = mod.UUID,
			MD5 = mod.MD5,
			LastModified = mod.LastModified
		};
		result.Dependencies.AddRange(mod.Dependencies.Items);
		return result;
	}
}
