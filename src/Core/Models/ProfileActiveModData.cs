using LSLib.LS;

namespace ModManager.Models;

public class ProfileActiveModData
{
	public string? Folder { get; set; }
	public string? MD5 { get; set; }
	public string? Name { get; set; }
	public string? UUID { get; set; }
	public ulong Version { get; set; }

	private static readonly NodeSerializationSettings _serializationSettings = new NodeSerializationSettings()
	{
		ByteSwapGuids = false,
		DefaultByteSwapGuids = false
	};

	private static string GetAttributeAsString(Dictionary<string, NodeAttribute> attributes, string name, string fallBack)
	{
		if (attributes.TryGetValue(name, out var attribute))
		{
			return attribute.AsString(_serializationSettings);
		}
		return fallBack;
	}

	private static ulong GetULongAttribute(Dictionary<string, NodeAttribute> attributes, string name, ulong fallBack)
	{
		if (attributes.TryGetValue(name, out var attribute))
		{
			if (attribute.Value is string att)
			{
				if (ulong.TryParse(att, out var val))
				{
					return val;
				}
				else
				{
					return fallBack;
				}
			}
			else if (attribute.Value is ulong val)
			{
				return val;
			}
		}
		return fallBack;
	}

	public void LoadFromAttributes(Dictionary<string, NodeAttribute> attributes)
	{
		Folder = GetAttributeAsString(attributes, "Folder", "");
		MD5 = GetAttributeAsString(attributes, "MD5", "");
		Name = GetAttributeAsString(attributes, "Name", "");
		UUID = GetAttributeAsString(attributes, "UUID", "");
		Version = GetULongAttribute(attributes, "Version", 0UL);

		//DivinityApp.LogMessage($"[DivinityProfileActiveModData] Name({Name}) UUID({UUID})");
	}
}
