namespace ModManager;

public class MenuSettingsAttribute(string parent = "", string displayName = "",
	bool addSeparatorAfter = false, string tooltip = "") : Attribute
{
	public string DisplayName { get; set; } = displayName;
	public string Parent { get; set; } = parent;
	public bool AddSeparator { get; set; } = addSeparatorAfter;
	public string ToolTip { get; set; } = tooltip;
	public string? Style { get; set; }
}
