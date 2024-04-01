using DynamicData.Binding;

namespace ModManager.Models.Mod;
public class ModCategory : ReactiveObject, IModEntry
{
	public ModEntryType EntryType => ModEntryType.Category;

	[Reactive] public string? UUID { get; set; }
	[Reactive] public string? DisplayName { get; set; }
	[Reactive] public int Index { get; set; }
	[Reactive] public bool IsActive { get; set; }
	[Reactive] public bool IsHidden { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsDraggable { get; set; }

	public string? Version => string.Empty;
	public string? Author => string.Empty;
	public string? LastUpdated => string.Empty;
	public bool CanDelete => true;

	public ObservableCollection<IModEntry> Mods { get; } = [];
	public IReadOnlyCollection<IModEntry>? Children => Mods;

	public string? Export(ModExportType exportType) => string.Empty;

	public ModCategory()
	{
		this.WhenAnyValue(x => x.IsHidden).Subscribe(b =>
		{
			if(!b) IsSelected = false;
		});
	}
}
