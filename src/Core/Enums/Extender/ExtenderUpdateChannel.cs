using System.ComponentModel;

namespace ModManager.Enums.Extender;

[JsonConverter(typeof(JsonStringEnumConverter<ExtenderUpdateChannel>))]
public enum ExtenderUpdateChannel
{
	[Description("Release")]
	Release,
	[Description("Devel")]
	Devel,
	[Description("Nightly")]
	Nightly
}
