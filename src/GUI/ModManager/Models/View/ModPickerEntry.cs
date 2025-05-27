using ModManager.Models.Interfaces;
using ModManager.Models.Mod;

namespace ModManager.Models.View;
public class ModPickerEntry : ReactiveObject, INamedEntry
{
	public ModData Mod { get; }
	public string UUID { get; }
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string? Name { get; }
	[ObservableAsProperty] public string? FilePath { get; }

	public ModPickerEntry(ModData mod)
	{
		Mod = mod;
		UUID = mod.UUID;

		mod.WhenAnyValue(x => x.Name).ToUIProperty(this, x => x.Name);
		mod.WhenAnyValue(x => x.FilePath).ToUIProperty(this, x => x.FilePath);
	}
}
