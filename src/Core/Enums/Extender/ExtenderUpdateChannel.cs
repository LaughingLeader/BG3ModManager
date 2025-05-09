using System.ComponentModel;

namespace ModManager.Enums.Extender;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExtenderUpdateChannel
{
	[Description("Release")]
	Release,
	[Description("Devel")]
	Devel
}
