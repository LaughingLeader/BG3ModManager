using DivinityModManager.Models.Mod;

using Newtonsoft.Json;

namespace DivinityModManager.Models;

[JsonObject(MemberSerialization.OptIn)]
public struct DivinityModDependencyData : IDivinityModData
{
	[JsonProperty] public string UUID { get; set; }
	[JsonProperty] public string Name { get; set; }
	public string Folder { get; set; }
	public string MD5 { get; set; }
	[JsonProperty] public LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; set; }

	public override string ToString() => $"Dependency|Name({Name}) UUID({UUID}) Version({Version?.Version})";

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
