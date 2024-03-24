using LSLib.LS;

namespace ModManager.Models;

public class DivinityProfileActiveModData
{
	public string Folder { get; set; }
	public string MD5 { get; set; }
	public string Name { get; set; }
	public string UUID { get; set; }
	public ulong Version { get; set; }

	private T GetAttribute<T>(Dictionary<string, NodeAttribute> attributes, string name, T fallBack)
	{
		if (attributes.TryGetValue(name, out var attribute))
		{
			var val = (T)attribute.Value;
			if (val != null)
			{
				return val;
			}
		}
		return fallBack;
	}

	private ulong GetULongAttribute(Dictionary<string, NodeAttribute> attributes, string name, ulong fallBack)
	{
		if (attributes.TryGetValue(name, out var attribute))
		{
			if (attribute.Value is string att)
			{
				if (UInt64.TryParse(att, out var val))
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
		Folder = GetAttribute<string>(attributes, "Folder", "");
		MD5 = GetAttribute<string>(attributes, "MD5", "");
		Name = GetAttribute<string>(attributes, "Name", "");
		UUID = GetAttribute<string>(attributes, "UUID", "");
		Version = GetULongAttribute(attributes, "Version", 0UL);

		//DivinityApp.LogMessage($"[DivinityProfileActiveModData] Name({Name}) UUID({UUID})");
	}
}
