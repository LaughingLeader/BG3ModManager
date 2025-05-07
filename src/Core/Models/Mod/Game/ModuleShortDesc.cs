using LSLib.LS;

using System.Runtime.Serialization;

namespace ModManager.Models.Mod.Game;

[DataContract]
public class ModuleShortDesc : ReactiveObject, IModData
{
	[DataMember, Reactive] public string UUID { get; set; }
	[DataMember, Reactive] public string? Name { get; set; }
	[DataMember, Reactive] public string? Folder { get; set; }
	[DataMember, Reactive] public string? MD5 { get; set; }
	[DataMember, Reactive] public ulong PublishHandle { get; set; }
	[DataMember, Reactive] public LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; set; }

	public override string ToString() => $"[ModuleShortDesc] Name({Name}) UUID({UUID}) Version({Version?.Version})";

	private static readonly NodeSerializationSettings _serializationSettings = new()
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

	public static ModuleShortDesc FromAttributes(Dictionary<string, NodeAttribute> attributes)
	{
		return new ModuleShortDesc(GetAttributeAsString(attributes, "UUID", "") ?? string.Empty)
		{
			Folder = GetAttributeAsString(attributes, "Folder", ""),
			MD5 = GetAttributeAsString(attributes, "MD5", ""),
			Name = GetAttributeAsString(attributes, "Name", ""),
			Version = new LarianVersion(GetULongAttribute(attributes, "Version", 0UL)),
		};
	}

	public static ModuleShortDesc FromModData(IModData m)
	{
		return new ModuleShortDesc(m.UUID)
		{
			Folder = m.Folder,
			Name = m.Name,
			MD5 = m.MD5,
			PublishHandle = m.PublishHandle,
			Version = m.Version,
			LastModified = m.LastModified
		};
	}

	public void UpdateFrom(IModData m)
	{
		Folder = m.Folder;
		Name = m.Name;
		UUID = m.UUID;
		MD5 = m.MD5;
		PublishHandle = m.PublishHandle;
		Version = m.Version;
		LastModified = m.LastModified;
	}

	public ModuleShortDesc(string uuid)
	{
		UUID = uuid;
		Name = "";
		Folder = "";
		MD5 = "";
		PublishHandle = 0ul;
		Version = new();
	}
}
