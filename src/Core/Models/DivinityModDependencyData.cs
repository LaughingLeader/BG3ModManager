using ModManager.Models.Mod;

using System.Runtime.Serialization;

namespace ModManager.Models;

[DataContract]
public struct DivinityModDependencyData : IDivinityModData
{
	[DataMember] public string? UUID { get; set; }
	[DataMember] public string? Name { get; set; }
	public string? Folder { get; set; }
	public string? MD5 { get; set; }
	[DataMember] public LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; set; }

	public readonly override string ToString() => $"Dependency|Name({Name}) UUID({UUID}) Version({Version?.Version})";

	public static DivinityModDependencyData FromModData(DivinityModData m)
	{
		return new DivinityModDependencyData
		{
			Folder = m.Folder,
			Name = m.Name,
			UUID = m.UUID,
			MD5 = m.MD5,
			Version = m.Version,
			LastModified = m.LastModified
		};
	}
}
