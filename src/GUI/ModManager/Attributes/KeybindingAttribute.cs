using Avalonia.Input;

namespace ModManager;
public class KeybindingAttribute : Attribute
{
	public string? DisplayName { get; set; }
	public string? MenuCategory { get; set; }

	public Key Key { get; set; }
	public ModifierKeys Modifiers { get; set; }

	public KeybindingAttribute(string displayName, Key key, ModifierKeys modifiers = KeyModifiers.None, string? menuCategory = null)
	{
		DisplayName = displayName;
		Key = key;
		Modifiers = modifiers;
		MenuCategory = menuCategory;
	}
}
