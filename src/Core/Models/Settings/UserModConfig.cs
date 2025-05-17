using DynamicData;

using ModManager.Json;
using ModManager.Models.Mod;

using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace ModManager.Models.Settings;

[DataContract]
public class UserModConfig : BaseSettings<UserModConfig>, ISerializableSettings
{
	[DataMember]
	[JsonConverter(typeof(DictionaryToSourceCacheConverter<ModConfig>))]
	public SourceCache<ModConfig, string> Mods { get; set; }

	[DataMember] public Dictionary<string, long> LastUpdated { get; set; }

	public UserModConfig() : base("usermodconfig.json")
	{
		Mods = new SourceCache<ModConfig, string>(x => x.Id);
		LastUpdated = [];
	}
}
