using System.ComponentModel;

namespace ModManager;

[JsonConverter(typeof(JsonStringEnumConverter<GameLaunchWindowAction>))]
public enum GameLaunchWindowAction
{
	[Description("Do nothing")]
	None,
	[Description("Minimize the window")]
	Minimize,
	[Description("Close the window")]
	Close
}
