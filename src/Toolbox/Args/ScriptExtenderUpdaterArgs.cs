using PowerArgs;

namespace Toolbox.Args;

public class ScriptExtenderUpdaterArgs
{
	[ArgShortcut("-u"), ArgDescription("The path to DWrite.dll"), ArgRequired]
	public string? Updater { get; set; }

	[ArgShortcut("-b"), ArgDescription("The path to the game's bin folder, where ScriptExtenderUpdaterConfig.json / bg3.ex is")]
	public string? BinFolder { get; set; }
}
