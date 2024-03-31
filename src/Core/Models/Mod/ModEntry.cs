using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models.Mod;
public class ModEntry : ReactiveObject, IModEntry
{
	public ModEntryType EntryType => ModEntryType.Mod;

	[Reactive] public int Index { get; set; }

	[Reactive] public bool IsActive { get; set; }
	[Reactive] public bool IsVisible { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsDraggable { get; set; }

	[ObservableAsProperty] public string? UUID { get; }
	[ObservableAsProperty] public string? DisplayName { get; }
	[ObservableAsProperty] public string? Version { get; }
	[ObservableAsProperty] public string? Author { get; }
	[ObservableAsProperty] public string? LastUpdated { get; }
	[ObservableAsProperty] public bool CanDelete { get; }

	public IReadOnlyCollection<IModEntry>? Children => null;

	public string? Export(ModExportType exportType) => string.Empty;

	[Reactive] public DivinityModData? Data { get; set; }

	public ModEntry()
	{
		var whenMod = this.WhenAnyValue(x => x.Data).WhereNotNull();

		whenMod.Select(x => x.Index).BindTo(this, x => x.Index);

		whenMod.Select(x => x.UUID).ToUIProperty(this, x => x.UUID);
		whenMod.Select(x => x.DisplayName).ToUIProperty(this, x => x.DisplayName);
		whenMod.Select(x => x.Version.Version).ToUIProperty(this, x => x.Version);
		whenMod.Select(x => x.Author).ToUIProperty(this, x => x.Author);
		whenMod.Select(x => x.LastModifiedDateText).ToUIProperty(this, x => x.LastUpdated);

		whenMod.Select(x => x.CanDelete).ToUIProperty(this, x => x.CanDelete);
	}

	public ModEntry(DivinityModData modData) : this()
	{
		Data = modData;
	}
}
