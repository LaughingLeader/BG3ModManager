using LSLib.LS;

using ModManager.Extensions;

using System.Reflection;

namespace ModManager.Models.Mod.Game;

public class ModuleInfo
{
	public string? Author;
	public string? CharacterCreationLevelName;
	public string? Description;
	public string? Folder;
	public string? LobbyLevelName;
	public string? MD5;
	public string? MainMenuBackgroundVideo;
	public string? MenuLevelName;
	public string? Name;
	public int NumPlayers;
	public string? PhotoBooth;
	public string? StartupLevelName;
	public string? Tags;
	public string? Type;
	public string? UUID;
	public long Version64;

	private static readonly NodeSerializationSettings nodeSerializationSettings = new();

	private static object TryGetAttribute(string property, Node node, Type fieldType)
	{
		if (node.Attributes.TryGetValue(property, out var nodeAttribute))
		{
			if (fieldType == typeof(string))
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

	private static readonly FieldInfo[] _fields = typeof(ModuleInfo).GetFields(BindingFlags.Instance | BindingFlags.Public);

	public static ModuleInfo FromResource(Resource res)
	{
		var meta = new ModuleInfo();
		if (res != null)
		{
			if (res.TryFindRegion("Config", out var region))
			{
				if (region.TryFindNode("ModuleInfo", out var moduleInfo))
				{
					foreach (var field in _fields)
					{
						var value = TryGetAttribute(field.Name, moduleInfo, field.FieldType);
						if (value != null)
						{
							field.SetValue(meta, value);
						}
					}
				}
			}
		}
		return meta;
	}
}
