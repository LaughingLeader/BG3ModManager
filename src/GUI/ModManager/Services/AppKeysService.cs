using DynamicData;

using ModManager.Models;

namespace ModManager;
public class AppKeysService
{
	public SourceCache<Hotkey, string> Hotkeys { get; } = new(x => x.Id);

	public void RegisterCommand(string id, string displayName, IReactiveCommand command, Key key = Key.None, ModifierKeys modifiers = KeyModifiers.None)
	{
		Hotkeys.AddOrUpdate(new Hotkey(id, displayName, key, command, modifiers));
	}
}
