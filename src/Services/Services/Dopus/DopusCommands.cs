namespace ModManager.Services.Dopus;
public static class DopusCommands
{
	/// <summary>
	/// Looks for an existing tab when opening a new tab, and focuses the lister window.
	/// </summary>
	private static readonly DopusCommandArg s_newTabDefault = new("NEWTAB", ["deflister", "findexisting"]);

	/// <summary>
	/// Looks for an existing tab when opening a new tab.
	/// </summary>
	private static readonly DopusCommandArg s_newTabDefaultWithFocus = new("NEWTAB", [.. s_newTabDefault.Args, "tofront"]);

	/// <summary>
	/// Open a given file in Dopus, using an existing tab if possible, and does not focus the lister.
	/// </summary>
	public static readonly DopusCommand OpenFileInNewTab = new("Go", [s_newTabDefault]);

	/// <summary>
	/// Open a given file in Dopus, using an existing tab if possible, and focuses the lister.
	/// </summary>
	public static readonly DopusCommand OpenFileInNewTabWithFocus = new("Go", [s_newTabDefaultWithFocus]);
}
