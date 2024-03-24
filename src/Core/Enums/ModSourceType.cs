using System.ComponentModel;

namespace ModManager;

public enum ModSourceType
{
	[Description("None")]
	NONE,
	[Description("GitHub")]
	GITHUB,
	[Description("Nexus Mods")]
	NEXUSMODS,
	[Description("Steam Workshop")]
	STEAM
}
