using ModManager.Json;
using ModManager.Models.Mod;

using DynamicData;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace ModManager.Models.Settings;

[DataContract]
public class UserModConfig : BaseSettings<UserModConfig>, ISerializableSettings
{
	[Newtonsoft.Json.JsonConverter(typeof(DictionaryToSourceCacheConverter<ModConfig>)), DataMember]
	public SourceCache<ModConfig, string> Mods { get; set; }

	[DataMember] public Dictionary<string, long> LastUpdated { get; set; }

	private ICommand AutosaveCommand { get; }

	public void TrySave()
	{
		this.Save(out _);
	}

	public UserModConfig() : base("usermodconfig.json")
	{
		Mods = new SourceCache<ModConfig, string>(x => x.Id);
		LastUpdated = new Dictionary<string, long>();

		var props = typeof(ModConfig)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(ReactiveAttribute)))
		.Select(prop => prop.Name)
		.ToArray();

		AutosaveCommand = ReactiveCommand.Create(TrySave);

		Mods.Connect().WhenAnyPropertyChanged(props).Throttle(TimeSpan.FromMilliseconds(25)).Select(x => Unit.Default).InvokeCommand(AutosaveCommand);
	}
}
