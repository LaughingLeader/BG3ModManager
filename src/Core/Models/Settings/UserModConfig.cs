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

	private ICommand AutosaveCommand { get; }

	public UserModConfig() : base("usermodconfig.json")
	{
		Mods = new SourceCache<ModConfig, string>(x => x.Id);
		LastUpdated = [];

		var props = typeof(ModConfig)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(ReactiveAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		AutosaveCommand = ReactiveCommand.Create(() =>
		{
			this.Save(out _);
		});

		Mods.Connect().WhenAnyPropertyChanged(props).Throttle(TimeSpan.FromMilliseconds(25)).Select(x => Unit.Default).InvokeCommand(AutosaveCommand);
	}
}
