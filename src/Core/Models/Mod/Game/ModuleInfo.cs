using LSLib.LS;

using ModManager.Extensions;

using System.Reflection;

namespace ModManager.Models.Mod.Game;

public class ModuleInfo
{
	public string? Author { get; set; }
	public string? CharacterCreationLevelName { get; set; }
	public string? Description { get; set; }
	public string? Folder { get; set; }
	public string? LobbyLevelName { get; set; }
	public string? MD5 { get; set; }
	public string? MainMenuBackgroundVideo { get; set; }
	public string? MenuLevelName { get; set; }
	public string? Name { get; set; }
	public int NumPlayers { get; set; }
	public string? PhotoBooth { get; set; }
	public string? StartupLevelName { get; set; }
	public string? Tags { get; set; }
	public string? Type { get; set; }
	public string? UUID { get; set; }
	public long Version64 { get; set; }

	private static readonly NodeSerializationSettings nodeSerializationSettings = new();

	private static object? TryGetAttribute(string property, Node node, Type propType)
	{
		if (node.Attributes.TryGetValue(property, out var nodeAttribute))
		{
			if (propType == typeof(string))
			{
				return nodeAttribute.AsString(nodeSerializationSettings);
			}
			else
			{
				return nodeAttribute.Value;
			}
		}
		return null;
	}

	private static readonly PropertyInfo[] _props = typeof(ModuleInfo).GetProperties(BindingFlags.Instance | BindingFlags.Public);

	public static ModuleInfo FromResource(Resource res)
	{
		var meta = new ModuleInfo();
		if (res != null)
		{
			if (res.TryFindRegion("Config", out var region))
			{
				if (region.TryFindNode("ModuleInfo", out var moduleInfo))
				{
					foreach (var prop in _props)
					{
						var value = TryGetAttribute(prop.Name, moduleInfo, prop.PropertyType);
						if (value != null)
						{
							prop.SetValue(meta, value);
						}
					}
				}
			}
		}
		return meta;
	}
}
