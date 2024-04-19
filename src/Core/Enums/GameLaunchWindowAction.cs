using System.ComponentModel;

namespace ModManager;

public enum GameLaunchWindowAction
{
	[Description("Do nothing")]
	None,
	[Description("Minimize the window")]
	Minimize,
	[Description("Close the window")]
	Close
}
