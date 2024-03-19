using DivinityModManager.Models.Mod;

using System.Runtime.Serialization;

namespace DivinityModManager.Models;

[DataContract]
public class DivinitySerializedModData : IDivinityModData
{
	[DataMember] public int Index { get; set; }
	[DataMember] public string FileName { get; set; }
	[DataMember] public string UUID { get; set; }
	[DataMember] public string Folder { get; set; }
	[DataMember] public string Name { get; set; }
	[DataMember] public string Description { get; set; }
	[DataMember] public string Author { get; set; }

	[DataMember] public LarianVersion Version { get; set; }

	[DataMember] public string Type { get; set; }
	[DataMember] public List<string> Modes { get; set; }

	[DataMember] public string Targets { get; set; }

	[DataMember] public DivinityModScriptExtenderConfig ScriptExtenderData { get; set; }
	[DataMember] public List<DivinityModDependencyData> Dependencies { get; set; }

	[DataMember] public string MD5 { get; set; }

	public DateTimeOffset? LastModified { get; set; }

	public static DivinitySerializedModData FromMod(DivinityModData mod)
	{
		return new DivinitySerializedModData
		{
			Author = mod.AuthorDisplayName,
			Dependencies = mod.Dependencies.Items.ToList(),
			Description = mod.Description,
			FileName = mod.FileName,
			Folder = mod.Folder,
			Name = mod.Name,
			Version = mod.Version,
			Type = mod.ModType,
			Modes = mod.Modes.ToList(),
			Targets = mod.Targets,
			Index = mod.Index,
			ScriptExtenderData = mod.ScriptExtenderData,
			UUID = mod.UUID,
			MD5 = mod.MD5,
			LastModified = mod.LastModified
		};
	}
}
